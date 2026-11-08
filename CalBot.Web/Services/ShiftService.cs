using CalBot.Core.Models;
using CalBot.Core.Services;
using CalBot.Data;
using Microsoft.EntityFrameworkCore;

namespace CalBot.Web.Services;

public class ShiftService : IShiftService
{
private readonly CalBotContext _db;

public ShiftService(CalBotContext db)
{
_db = db;
}

public async Task<List<Shift>> GetShiftsAsync(long userId)
{
return await _db.Shifts
.AsNoTracking()
.Where(s => s.UserId == userId)
.OrderByDescending(s => s.StartTime)
.ToListAsync();
}

public async Task<Shift?> GetShiftAsync(long userId, int id)
{
return await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
}

public async Task<Shift> AddShiftAsync(Shift shift)
{
_db.Shifts.Add(shift);
await _db.SaveChangesAsync();
return shift;
}

public async Task UpdateShiftAsync(Shift shift)
{
_db.Shifts.Update(shift);
await _db.SaveChangesAsync();
}

public async Task DeleteShiftAsync(long userId, int id)
{
var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
if (shift is not null)
{
_db.Shifts.Remove(shift);
await _db.SaveChangesAsync();
}
}

public async Task TogglePaidAsync(long userId, int id)
{
var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
if (shift is not null)
{
shift.IsPaid = !shift.IsPaid;
await _db.SaveChangesAsync();
}
}

public decimal CalculateHours(DateTime start, DateTime end)
{
if (end < start)
end = end.AddDays(1); // переход через полночь

var duration = end - start;
return (decimal)duration.TotalHours;
}

public decimal CalculateGross(decimal hours, decimal hourlyRate) => hours * hourlyRate;

public decimal CalculateTax(decimal gross) => gross * 0.06m;

public decimal CalculateNet(decimal gross, decimal tax) => gross - tax;

public decimal CalculateDebtPart(decimal net) => net * 0.8m;

public decimal CalculateLifePart(decimal net) => net * 0.2m;

public DateTime CalculateExpectedPaymentDate(DateTime startTime) => startTime.AddDays(14);
}