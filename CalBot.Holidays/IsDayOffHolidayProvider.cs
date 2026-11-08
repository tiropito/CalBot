using System.Globalization;
using CalBot.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CalBot.Holidays;

/// <summary>
/// Клиент isdayoff.ru: "0" (рабочий) / "1" (выходной), с учётом переносов в РФ.
/// </summary>
public class IsDayOffHolidayProvider : IHolidayProvider
{
private readonly HttpClient _http;
private readonly IMemoryCache _cache;
private readonly ILogger<IsDayOffHolidayProvider> _logger;

public IsDayOffHolidayProvider(
HttpClient http,
IMemoryCache cache,
ILogger<IsDayOffHolidayProvider> logger)
{
_http = http;
_cache = cache;
_logger = logger;
}

public async Task<bool> IsDayOffAsync(DateOnly date, CancellationToken ct = default)
{
var key = $"isdayoff:{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

if (_cache.TryGetValue(key, out bool cached))
return cached;

try
{
var body = await _http.GetStringAsync($"{date:yyyyMMdd}?cc=ru", ct);
var isDayOff = body.Trim() == "1";
_cache.Set(key, isDayOff, TimeSpan.FromHours(24));
return isDayOff;
}
catch (Exception ex)
{
_logger.LogWarning(ex, "isdayoff.ru unavailable for {Date}, weekend fallback", date);
var fallback = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
_cache.Set(key, fallback, TimeSpan.FromMinutes(5));
return fallback;
}
}
}