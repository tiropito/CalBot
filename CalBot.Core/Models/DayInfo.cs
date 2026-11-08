namespace CalBot.Core.Models;

public record DayInfo(
    DateOnly Date,
    int Week,
    IReadOnlyList<Lesson> Lessons,
    IReadOnlyList<TaskItem> Tasks,
    IReadOnlyList<TaskItem> SpilloverTasks,
    bool IsHoliday)
{
    public bool IsFree => !IsHoliday
                          && Lessons.Count == 0
                          && Tasks.Count == 0
                          && SpilloverTasks.Count == 0;

    public bool HasSpecialTask => Tasks.Any(t => t.IsSpecial);
}