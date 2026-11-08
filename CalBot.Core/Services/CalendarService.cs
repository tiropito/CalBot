using CalBot.Core.Models;

namespace CalBot.Core.Services;

public class CalendarService
{
    private readonly ICalendarRepository _repo;
    private readonly IHolidayProvider _holidays;
    private readonly AcademicCalendar _academic;

    public CalendarService(ICalendarRepository repo, IHolidayProvider holidays, AcademicCalendar academic)
    {
        _repo = repo;
        _holidays = holidays;
        _academic = academic;
    }

    public async Task<IReadOnlyList<DayInfo>> BuildAsync(
        long userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
            throw new ArgumentException("'to' must be >= 'from'.");

        var lessons = await _repo.GetLessonsAsync(userId, ct);

        // Забираем на день раньше — чтобы поймать задачи, начавшиеся вчера и заходящие на сегодня.
        var tasks = await _repo.GetTasksAsync(userId, from.AddDays(-1), to, ct);

        var result = new List<DayInfo>();

        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var week = _academic.WeekNumber(d);

            var dayLessons = lessons
                .Where(l => l.Week == week && l.Weekday == d.DayOfWeek)
                .OrderBy(l => l.Slot)
                .ToList();

            var dayTasks = tasks
                .Where(t => t.Date == d)
                .OrderBy(t => t.Time ?? TimeOnly.MaxValue)
                .ThenBy(t => t.Id)
                .ToList();

            // Ночные задачи: начались вчера, конец < начало (значит, переход через полночь).
            var spilloverTasks = tasks
                .Where(t => t.Date.AddDays(1) == d
                            && t.Time.HasValue
                            && t.EndTime.HasValue
                            && t.EndTime.Value < t.Time.Value)
                .OrderBy(t => t.EndTime)
                .ThenBy(t => t.Id)
                .ToList();

            var isHoliday = await _holidays.IsDayOffAsync(d, ct);

            result.Add(new DayInfo(d, week, dayLessons, dayTasks, spilloverTasks, isHoliday));
        }

        return result;
    }
}