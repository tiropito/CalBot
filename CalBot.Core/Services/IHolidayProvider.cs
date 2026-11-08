namespace CalBot.Core.Services;

public interface IHolidayProvider
{
Task<bool> IsDayOffAsync(DateOnly date, CancellationToken ct = default);
}