using CalBot.Core.Models;

namespace CalBot.Core.Services;

public interface IShiftService
{
Task<List<Shift>> GetShiftsAsync(long userId);
Task<Shift?> GetShiftAsync(long userId, int id);
Task<Shift> AddShiftAsync(Shift shift);
Task UpdateShiftAsync(Shift shift);
Task DeleteShiftAsync(long userId, int id);
Task TogglePaidAsync(long userId, int id);

decimal CalculateHours(DateTime start, DateTime end);
decimal CalculateGross(decimal hours, decimal hourlyRate);
decimal CalculateTax(decimal gross);
decimal CalculateNet(decimal gross, decimal tax);
decimal CalculateDebtPart(decimal net);
decimal CalculateLifePart(decimal net);
DateTime CalculateExpectedPaymentDate(DateTime startTime);
}