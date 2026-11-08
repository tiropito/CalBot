using CalBot.Core.Models;

namespace CalBot.Core.Services;

public interface ICalendarRepository
{
Task<IReadOnlyList<Lesson>> GetLessonsAsync(long userId, CancellationToken ct = default);
Task<IReadOnlyList<TaskItem>> GetTasksAsync(long userId, DateOnly from, DateOnly to, CancellationToken ct = default);
}