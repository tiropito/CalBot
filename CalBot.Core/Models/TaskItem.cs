namespace CalBot.Core.Models;

public class TaskItem
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string Text { get; set; } = string.Empty;
    public Priority Priority { get; set; } = Priority.Normal;
    public bool Done { get; set; }
    public bool IsSpecial { get; set; }
}