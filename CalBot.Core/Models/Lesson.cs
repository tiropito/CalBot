namespace CalBot.Core.Models;

public class Lesson
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public int Week { get; set; }
    public DayOfWeek Weekday { get; set; }
    public int Slot { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Teacher { get; set; }
    public string? Room { get; set; }
}