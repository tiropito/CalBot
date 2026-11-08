namespace CalBot.Core.Models;

public class Shift
{
public int Id { get; set; }
public long UserId { get; set; }
public DateTime StartTime { get; set; }
public DateTime EndTime { get; set; }
public decimal HourlyRate { get; set; }
public bool IsPaid { get; set; } = false;
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}