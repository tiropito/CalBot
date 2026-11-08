using CalBot.Core.Options;
using Microsoft.Extensions.Options;

namespace CalBot.Core.Services;

/// <summary>
/// Расчёт чётности учебной недели (верхняя/нижняя).
/// </summary>
public class AcademicCalendar
{
private readonly AcademicCalendarOptions _opt;

public AcademicCalendar(IOptions<AcademicCalendarOptions> options)
{
_opt = options.Value;
if (_opt.AnchorWeek is not (1 or 2))
throw new InvalidOperationException("AnchorWeek must be 1 or 2.");
}

/// <summary>Возвращает 1 или 2 — номер учебной недели для указанной даты.</summary>
public int WeekNumber(DateOnly date)
{
// Math.Floor важен: целочисленное деление в C# округляет К НУЛЮ,
// а не вниз. Для дат до опорной точки это даёт неверный сдвиг.
var deltaWeeks = (int)Math.Floor((date.DayNumber - _opt.AnchorDate.DayNumber) / 7.0);

// Остаток от деления в C# может быть отрицательным — нормализуем в [0;1].
var parity = ((deltaWeeks + _opt.AnchorWeek - 1) % 2 + 2) % 2;

return parity + 1;
}
}