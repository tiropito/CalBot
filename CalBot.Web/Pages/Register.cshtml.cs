using System.Security.Claims;
using System.Text.RegularExpressions;
using CalBot.Core.Models;
using CalBot.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CalBot.Web.Pages;

public partial class RegisterModel : PageModel
{
    private readonly CalBotContext _db;

    public RegisterModel(CalBotContext db)
    {
        _db = db;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string PasswordConfirm { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect("/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var username = Username?.Trim() ?? "";

        if (username.Length < 3 || username.Length > 32)
        {
            ErrorMessage = "Логин должен быть от 3 до 32 символов.";
            return Page();
        }

        if (!UsernameRegex().IsMatch(username))
        {
            ErrorMessage = "Логин может содержать только буквы, цифры, _ и -.";
            return Page();
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Пароль должен быть минимум 6 символов.";
            return Page();
        }

        if (Password != PasswordConfirm)
        {
            ErrorMessage = "Пароли не совпадают.";
            return Page();
        }

        if (await _db.Users.AnyAsync(u => u.Username == username))
        {
            ErrorMessage = "Такой логин уже занят.";
            return Page();
        }

        var isFirstUser = !await _db.Users.AnyAsync();

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Первый зарегавшийся получает «осиротевшие» данные с UserId=0.
        if (isFirstUser)
        {
            var orphanLessons = await _db.Lessons.Where(l => l.UserId == 0).ToListAsync();
            foreach (var l in orphanLessons) l.UserId = user.Id;

            var orphanTasks = await _db.Tasks.Where(t => t.UserId == 0).ToListAsync();
            foreach (var t in orphanTasks) t.UserId = user.Id;

            await _db.SaveChangesAsync();
        }

        // Логиним сразу после регистрации.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        return LocalRedirect("/");
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\-]+$")]
    private static partial Regex UsernameRegex();
}