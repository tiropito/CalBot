namespace CalBot.Core.Options;

/// <summary>
/// Точка привязки для расчёта чётности недель.
/// AnchorDate — дата, про которую ТОЧНО известно, что это неделя AnchorWeek.
/// </summary>
public class AcademicCalendarOptions
{
public DateOnly AnchorDate { get; set; } = new(2026, 9, 1);
public int AnchorWeek { get; set; } = 1;
}