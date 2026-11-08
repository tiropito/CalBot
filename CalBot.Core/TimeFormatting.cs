using System.Globalization;

namespace CalBot.Core;

public static class TimeFormatting
{
    /// <summary>
    /// Гибкий парсинг времени из строки. Понимает "9:00", "09:00", "09:00:00".
    /// </summary>
    public static TimeOnly? Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        var formats = new[] { "HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss" };
        if (TimeOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var t))
            return t;

        if (TimeOnly.TryParse(s, CultureInfo.InvariantCulture, out t))
            return t;

        return null;
    }

    /// <summary>
    /// Формат времени одного интервала. Если конец меньше начала — считаем,
    /// что задача переходит через полночь, и добавляем пометку "+1".
    /// </summary>
    public static string Range(TimeOnly? start, TimeOnly? end)
    {
        if (start is null && end is null) return string.Empty;

        if (start is not null && end is not null)
        {
            var overnight = end.Value < start.Value ? " (+1)" : "";
            return $"{start:HH\\:mm} – {end:HH\\:mm}{overnight}";
        }

        if (start is not null) return $"{start:HH\\:mm}";
        return $"до {end:HH\\:mm}";
    }
}