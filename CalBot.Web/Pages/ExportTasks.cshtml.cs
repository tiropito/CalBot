using System.Globalization;
using System.Security.Claims;
using System.Text;
using CalBot.Core.Models;
using CalBot.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CalBot.Web.Pages;

[Authorize]
public class ExportTasksModel : PageModel
{
    private readonly CalBotContext _db;

    public ExportTasksModel(CalBotContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> OnGetAsync(DateOnly? from, DateOnly? to, bool onlyPending = false)
    {
        var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null || !long.TryParse(claim.Value, out var userId))
            return Redirect("/login");

        if (from is null || to is null)
            return BadRequest("Укажи обе даты — 'from' и 'to'.");

        if (to < from)
            return BadRequest("'to' должен быть >= 'from'.");

        var query = _db.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= from && t.Date <= to);

        if (onlyPending)
            query = query.Where(t => !t.Done);

        var tasks = await query
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Time ?? TimeOnly.MaxValue)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.Append("Дата;Время;Окончание;Задача;Приоритет;Спецзадача;Выполнено\r\n");

        foreach (var t in tasks)
        {
            sb.Append(Escape(t.Date.ToString("dd.MM.yyyy")));
            sb.Append(';');
            sb.Append(Escape(t.Time?.ToString("HH\\:mm") ?? ""));
            sb.Append(';');
            sb.Append(Escape(t.EndTime?.ToString("HH\\:mm") ?? ""));
            sb.Append(';');
            sb.Append(Escape(t.Text));
            sb.Append(';');
            sb.Append(Escape(PriorityName(t.Priority)));
            sb.Append(';');
            sb.Append(t.IsSpecial ? "Да" : "Нет");
            sb.Append(';');
            sb.Append(t.Done ? "Да" : "Нет");
            sb.Append("\r\n");
        }

        // UTF-8 с BOM — без него Windows-Excel показывает кириллицу кракозябрами.
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[preamble.Length + body.Length];
        preamble.CopyTo(bytes, 0);
        body.CopyTo(bytes, preamble.Length);

        var fileName = $"tasks_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static string PriorityName(Priority p) => p switch
    {
        Priority.High => "Высокий",
        Priority.Low => "Низкий",
        _ => "Обычный"
    };

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}