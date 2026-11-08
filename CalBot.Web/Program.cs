using CalBot.Core;
using CalBot.Core.Options;
using CalBot.Core.Services;
using CalBot.Data;
using CalBot.Holidays;
using CalBot.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using CalBot.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Конфигурация ----------
var conn = builder.Configuration.GetConnectionString("Postgres")
           ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

// ---------- Сервисы ----------
builder.Services.AddDbContext<CalBotContext>(opt => opt.UseNpgsql(conn));
builder.Services.AddScoped<ICalendarRepository>(sp => sp.GetRequiredService<CalBotContext>());

builder.Services.Configure<AcademicCalendarOptions>(
    builder.Configuration.GetSection("AcademicCalendar"));
builder.Services.AddSingleton<AcademicCalendar>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<IShiftService, ShiftService>();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IHolidayProvider, IsDayOffHolidayProvider>(c =>
{
    c.BaseAddress = new Uri("https://isdayoff.ru/");
    c.Timeout = TimeSpan.FromSeconds(5);
});

// ---------- Auth ----------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/login";
        opt.LogoutPath = "/logout";
        opt.AccessDeniedPath = "/login";
        opt.ExpireTimeSpan = TimeSpan.FromDays(30);
        opt.SlidingExpiration = true;
        opt.Cookie.Name = "calbot_auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();

// ---------- Blazor + Razor Pages ----------
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorPages();

var app = builder.Build();

// ---------- Миграции при старте ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CalBotContext>();
    await db.Database.MigrateAsync();
}

// ---------- Pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapRazorPages();

await app.RunAsync();