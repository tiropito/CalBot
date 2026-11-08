using CalBot.Core.Models;
using CalBot.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace CalBot.Data;

public class CalBotContext(DbContextOptions<CalBotContext> options)
: DbContext(options), ICalendarRepository
{
public DbSet<Lesson> Lessons => Set<Lesson>();
public DbSet<TaskItem> Tasks => Set<TaskItem>();
public DbSet<User> Users => Set<User>();
public DbSet<Shift> Shifts => Set<Shift>();

protected override void OnModelCreating(ModelBuilder mb)
{
mb.Entity<Lesson>(e =>
{
e.HasKey(x => x.Id);
e.Property(x => x.Subject).HasMaxLength(200).IsRequired();
e.Property(x => x.Room).HasMaxLength(50);
e.Property(x => x.Week).IsRequired();
e.Property(x => x.Slot).IsRequired();

e.HasIndex(x => new { x.UserId, x.Week, x.Weekday, x.Slot }).IsUnique();    
mb.Entity<User>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.Username).HasMaxLength(50).IsRequired();
    e.HasIndex(x => x.Username).IsUnique();
    e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
});
});

mb.Entity<Shift>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.HourlyRate).HasColumnType("decimal(18,2)");
    e.HasIndex(x => new { x.UserId, x.StartTime });
});

mb.Entity<TaskItem>(e =>
{
e.HasKey(x => x.Id);
e.Property(x => x.Text).HasMaxLength(500).IsRequired();
e.HasIndex(x => new { x.UserId, x.Date });
e.HasIndex(x => new { x.UserId, x.Done });
});
}

async Task<IReadOnlyList<Lesson>> ICalendarRepository.GetLessonsAsync(long userId, CancellationToken ct)
=> await Lessons.AsNoTracking().Where(l => l.UserId == userId).ToListAsync(ct);

async Task<IReadOnlyList<TaskItem>> ICalendarRepository.GetTasksAsync(
long userId, DateOnly from, DateOnly to, CancellationToken ct)
=> await Tasks.AsNoTracking()
.Where(t => t.UserId == userId && t.Date >= from && t.Date <= to)
.ToListAsync(ct);
}