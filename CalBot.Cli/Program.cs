using System.Globalization;
using System.Text.Json;
using CalBot.Core;
using CalBot.Core.Models;
using CalBot.Core.Options;
using CalBot.Core.Services;
using CalBot.Data;
using CalBot.Holidays;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


var cmd = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";

var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
           ?? "Host=localhost;Port=5433;Database=calbot;Username=calbot;Password=calbot_dev";

var options = new DbContextOptionsBuilder<CalBotContext>().UseNpgsql(conn).Options;
await using var db = new CalBotContext(options);

var academic = new AcademicCalendar(Options.Create(new AcademicCalendarOptions
{
    AnchorDate = new DateOnly(2026, 10, 5),
    AnchorWeek = 1
}));

var cache = new MemoryCache(new MemoryCacheOptions());
var http = new HttpClient
{
    BaseAddress = new Uri("https://isdayoff.ru/"),
    Timeout = TimeSpan.FromSeconds(5)
};
using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole());
var holidays = new IsDayOffHolidayProvider(
    http, cache, loggerFactory.CreateLogger<IsDayOffHolidayProvider>());

var calendar = new CalendarService(db, holidays, academic);

try
{
    switch (cmd)
    {
        case "help":
        case "-h":
        case "--help":
            PrintHelp();
            break;

        case "week":
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            Console.WriteLine($"{today:dd.MM.yyyy} - week {academic.WeekNumber(today)}");
            break;
        }

        case "days":
        {
            var n = args.Length >= 2 && int.TryParse(args[1], out var parsed) ? parsed : 7;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var list = await calendar.BuildAsync(AppConstants.SingleUserId, today, today.AddDays(n - 1));

            foreach (var d in list)
            {
                Console.WriteLine($"{d.Date:dd.MM.yyyy} week={d.Week}");
                foreach (var l in d.Lessons)
                    Console.WriteLine($"    {l.Slot}. {l.Subject} ({l.Room})");
                foreach (var t in d.Tasks)
                    Console.WriteLine($"    task: {t.Text}");
            }
            break;
        }

        case "free":
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var list = await calendar.BuildAsync(AppConstants.SingleUserId, today, today.AddDays(60));
            var free = list.FirstOrDefault(d => d.IsFree);
            Console.WriteLine(free is null
                ? "No free day in the next 60 days."
                : $"Next free day: {free.Date:dd.MM.yyyy} ({free.Date.DayOfWeek})");
            break;
        }

        case "seed":
        {
            var path = args.Length >= 2 ? args[1] : "sample-lessons.json";

            if (!File.Exists(path))
            {
                Console.WriteLine($"File not found: {path}");
                return;
            }

            // Определяем целевого пользователя.
            long userId;
            var userArg = Environment.GetEnvironmentVariable("CALBOT_USER_ID");
            var parsedFlag = args.Skip(2).FirstOrDefault(a => a.StartsWith("--user="))?[7..];
            if (!string.IsNullOrWhiteSpace(parsedFlag) && long.TryParse(parsedFlag, out var uidFromFlag))
                userId = uidFromFlag;
            else if (!string.IsNullOrWhiteSpace(userArg) && long.TryParse(userArg, out var uidFromEnv))
                userId = uidFromEnv;
            else
            {
                var first = await db.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
                if (first is null)
                {
                    Console.WriteLine("No users registered yet. Register in the web app first, or pass --user=N.");
                    return;
                }
                userId = first.Id;
            }

            Console.WriteLine($"Seeding for user id = {userId}");

            var json = await File.ReadAllTextAsync(path);
            var dtos = JsonSerializer.Deserialize<List<LessonDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Empty or broken JSON.");

            var existing = await db.Lessons.Where(l => l.UserId == userId).ToListAsync();
            db.Lessons.RemoveRange(existing);

            foreach (var dto in dtos)
            {
                db.Lessons.Add(new Lesson
                {
                    UserId = userId,
                    Week = dto.Week,
                    Weekday = Enum.Parse<DayOfWeek>(dto.Weekday, ignoreCase: true),
                    Slot = dto.Slot,
                    StartTime = string.IsNullOrWhiteSpace(dto.StartTime)
                        ? null
                        : TimeOnly.Parse(dto.StartTime, CultureInfo.InvariantCulture),
                    EndTime = string.IsNullOrWhiteSpace(dto.EndTime)
                        ? null
                        : TimeOnly.Parse(dto.EndTime, CultureInfo.InvariantCulture),
                    Subject = dto.Subject,
                    Teacher = dto.Teacher,
                    Room = dto.Room
                });
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"Loaded {dtos.Count} lessons for user {userId}. Old schedule for this user removed.");
            break;
        }

        default:
            Console.WriteLine($"Unknown command: {cmd}");
            PrintHelp();
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

void PrintHelp()
{
    Console.WriteLine(
        """
        CalBot CLI
        Usage:
          dotnet run -- week
          dotnet run -- days N
          dotnet run -- free
          dotnet run -- seed lessons.json
        """);
}

internal record LessonDto(
    int Week,
    string Weekday,
    int Slot,
    string Subject,
    string? Teacher,
    string? Room,
    string? StartTime,
    string? EndTime);