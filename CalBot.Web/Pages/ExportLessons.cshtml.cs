using System.Globalization;
using System.Security.Claims;
using System.Text;
using CalBot.Core.Models;
using CalBot.Core.Services;
using CalBot.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CalBot.Web.Pages;

[Authorize]
public class ExportLessonsModel : PageModel
{
    private readonly CalBotContext _db;
    private readonly AcademicCalendar _academic;

    public ExportLessonsModel(CalBotContext db, AcademicCalendar academic)
    {
        _db = db;
        _academic = academic;
    }

    public async Task<IActionResult> OnGetAsync(
        DateOnly? from, DateOnly? to, bool template = false)
    {
        var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null || !long.TryParse(claim.Value, out var userId))
            return Redirect("/login");

        var lessons = await _db.Lessons
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Week).ThenBy(l => l.Weekday).ThenBy(l => l.Slot)
            .ToListAsync();

        var sb = new StringBuilder();

        // ---------- Шаблон (без дат) ----------
        if (template)
        {
            sb.Append("Неделя;День;Пара;Начало;Окончание;Предмет;Преподаватель;Аудитория\r\n");

            foreach (var l in lessons)
            {
                sb.Append(l.Week); sb.Append(';');
                sb.Append(Escape(DayName(l.Weekday))); sb.Append(';');
                sb.Append(l.Slot); sb.Append(';');
                sb.Append(Escape(l.StartTime?.ToString("HH\\:mm") ?? "")); sb.Append(';');
                sb.Append(Escape(l.EndTime?.ToString("HH\\:mm") ?? "")); sb.Append(';');
                sb.Append(Escape(l.Subject)); sb.Append(';');
                sb.Append(Escape(l.Teacher ?? "")); sb.Append(';');
                sb.Append(Escape(l.Room ?? "")); sb.Append("\r\n");
            }

            return BuildCsvResponse(sb, "lessons_template.csv");
        }

        // ---------- На период (с реальными датами) ----------
        if (from is null || to is null)
            return BadRequest("Укажи обе даты — 'from' и 'to'.");

        if (to < from)
            return BadRequest("'to' должен быть >= 'from'.");

        sb.Append("Дата;День;Неделя;Пара;Начало;Окончание;Предмет;Преподаватель;Аудитория\r\n");

        for (var d = from.Value; d <= to.Value; d = d.AddDays(1))
        {
            var week = _academic.WeekNumber(d);
            var dayLessons = lessons
                .Where(l => l.Week == week && l.Weekday == d.DayOfWeek)
                .OrderBy(l => l.Slot)
                .ToList();

            foreach (var l in dayLessons)
            {
                sb.Append(Escape(d.ToString("dd.MM.yyyy"))); sb.Append(';');
                sb.Append(Escape(DayName(d.DayOfWeek))); sb.Append(';');
                sb.Append(week); sb.Append(';');
                sb.Append(l.Slot); sb.Append(';');
                sb.Append(Escape(l.StartTime?.ToString("HH\\:mm") ?? "")); sb.Append(';');
                sb.Append(Escape(l.EndTime?.ToString("HH\\:mm") ?? "")); sb.Append(';');
                sb.Append(Escape(l.Subject)); sb.Append(';');
                sb.Append(Escape(l.Teacher ?? "")); sb.Append(';');
                sb.Append(Escape(l.Room ?? "")); sb.Append("\r\n");
            }
        }

        var fileName = $"lessons_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.csv";
        return BuildCsvResponse(sb, fileName);
    }

    private static FileContentResult BuildCsvResponse(StringBuilder sb, string fileName)
    {
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[preamble.Length + body.Length];
        preamble.CopyTo(bytes, 0);
        body.CopyTo(bytes, preamble.Length);

        return new FileContentResult(bytes, "text/csv; charset=utf-8")
        {
            FileDownloadName = fileName
        };
    }

    private static string DayName(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "Понедельник",
        DayOfWeek.Tuesday => "Вторник",
        DayOfWeek.Wednesday => "Среда",
        DayOfWeek.Thursday => "Четверг",
        DayOfWeek.Friday => "Пятница",
        DayOfWeek.Saturday => "Суббота",
        DayOfWeek.Sunday => "Воскресенье",
        _ => "?"
    };

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}