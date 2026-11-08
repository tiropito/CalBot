using System.Security.Claims;
using CalBot.Core.Models;
using CalBot.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CalBot.Web.Pages;

public class AccountModel : PageModel
{
    private readonly CalBotContext _db;

    public AccountModel(CalBotContext db)
    {
        _db = db;
    }

    public User? CurrentUser { get; set; }
    public int LessonCount { get; set; }
    public int TaskCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PasswordMessage { get; set; }
    public bool PasswordSuccess { get; set; }

    [BindProperty]
    public string ConfirmUsername { get; set; } = string.Empty;

    [BindProperty]
    public string OldPassword { get; set; } = string.Empty;

    [BindProperty]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    public string NewPasswordConfirm { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();
        if (userId is null)
            return Redirect("/login");

        CurrentUser = await _db.Users.FindAsync((int)userId.Value);
        if (CurrentUser is null)
            return Redirect("/login");

        LessonCount = await _db.Lessons.CountAsync(l => l.UserId == userId.Value);
        TaskCount = await _db.Tasks.CountAsync(t => t.UserId == userId.Value && !t.Done);

        return Page();
    }

    // ---- Смена пароля ----

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var userId = GetUserId();
        if (userId is null)
            return Redirect("/login");

        var user = await _db.Users.FindAsync((int)userId.Value);
        if (user is null)
            return Redirect("/login");

        CurrentUser = user;
        LessonCount = await _db.Lessons.CountAsync(l => l.UserId == userId.Value);
        TaskCount = await _db.Tasks.CountAsync(t => t.UserId == userId.Value && !t.Done);

        if (string.IsNullOrWhiteSpace(OldPassword) ||
            !BCrypt.Net.BCrypt.Verify(OldPassword, user.PasswordHash))
        {
            PasswordMessage = "Текущий пароль неверный.";
            PasswordSuccess = false;
            return Page();
        }

        if (NewPassword.Length < 6)
        {
            PasswordMessage = "Новый пароль должен быть минимум 6 символов.";
            PasswordSuccess = false;
            return Page();
        }

        if (NewPassword != NewPasswordConfirm)
        {
            PasswordMessage = "Новые пароли не совпадают.";
            PasswordSuccess = false;
            return Page();
        }

        if (NewPassword == OldPassword)
        {
            PasswordMessage = "Новый пароль совпадает со старым. Придумай другой.";
            PasswordSuccess = false;
            return Page();
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        await _db.SaveChangesAsync();

        // Разлогиниваем и редиректим на логин, чтобы все сессии сбросились.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }

    // ---- Удаление аккаунта ----

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = GetUserId();
        if (userId is null)
            return Redirect("/login");

        var user = await _db.Users.FindAsync((int)userId.Value);
        if (user is null)
            return Redirect("/login");

        if (!string.Equals(ConfirmUsername.Trim(), user.Username, StringComparison.Ordinal))
        {
            CurrentUser = user;
            LessonCount = await _db.Lessons.CountAsync(l => l.UserId == userId.Value);
            TaskCount = await _db.Tasks.CountAsync(t => t.UserId == userId.Value && !t.Done);
            ErrorMessage = "Логин введён неверно. Аккаунт не удалён.";
            return Page();
        }

        var lessons = await _db.Lessons.Where(l => l.UserId == userId.Value).ToListAsync();
        _db.Lessons.RemoveRange(lessons);

        var tasks = await _db.Tasks.Where(t => t.UserId == userId.Value).ToListAsync();
        _db.Tasks.RemoveRange(tasks);

        var shifts = await _db.Shifts.Where(s => s.UserId == userId.Value).ToListAsync();
        _db.Shifts.RemoveRange(shifts);

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/login");
    }

    private long? GetUserId()
    {
        var idClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (idClaim is null || !long.TryParse(idClaim.Value, out var id))
            return null;
        return id;
    }
}