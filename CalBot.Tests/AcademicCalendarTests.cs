using CalBot.Core.Options;
using CalBot.Core.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace CalBot.Tests;

public class AcademicCalendarTests
{
private static AcademicCalendar MakeCalendar(DateOnly anchor, int anchorWeek = 1)
=> new(Options.Create(new AcademicCalendarOptions
{
AnchorDate = anchor,
AnchorWeek = anchorWeek
}));

[Fact]
public void Anchor_date_is_week_1()
{
var anchor = new DateOnly(2026, 9, 1);
Assert.Equal(1, MakeCalendar(anchor).WeekNumber(anchor));
}

[Fact]
public void Same_week_as_anchor_is_week_1()
{
var anchor = new DateOnly(2026, 9, 1);
var cal = MakeCalendar(anchor);
Assert.Equal(1, cal.WeekNumber(new DateOnly(2026, 9, 5)));
Assert.Equal(2, cal.WeekNumber(new DateOnly(2026, 9, 7)));
}

[Fact]
public void Alternates_every_7_days()
{
var anchor = new DateOnly(2026, 9, 1);
var cal = MakeCalendar(anchor);
Assert.Equal(1, cal.WeekNumber(anchor));
Assert.Equal(2, cal.WeekNumber(anchor.AddDays(7)));
Assert.Equal(1, cal.WeekNumber(anchor.AddDays(14)));
Assert.Equal(2, cal.WeekNumber(anchor.AddDays(21)));
Assert.Equal(1, cal.WeekNumber(anchor.AddDays(28)));
}

[Fact]
public void Works_for_dates_before_anchor()
{
var anchor = new DateOnly(2026, 9, 1);
var cal = MakeCalendar(anchor);
Assert.Equal(2, cal.WeekNumber(anchor.AddDays(-7)));
Assert.Equal(1, cal.WeekNumber(anchor.AddDays(-14)));
Assert.Equal(2, cal.WeekNumber(anchor.AddDays(-21)));
}

[Fact]
public void Anchor_week_2_shifts_parity()
{
var anchor = new DateOnly(2026, 9, 1);
var cal = MakeCalendar(anchor, anchorWeek: 2);
Assert.Equal(2, cal.WeekNumber(anchor));
Assert.Equal(1, cal.WeekNumber(anchor.AddDays(7)));
Assert.Equal(2, cal.WeekNumber(anchor.AddDays(14)));
}

[Fact]
public void Whole_semester_alternates_correctly()
{
var anchor = new DateOnly(2026, 9, 1);
var cal = MakeCalendar(anchor);
var current = 1;
for (var d = anchor; d <= anchor.AddDays(120); d = d.AddDays(7))
{
Assert.Equal(current, cal.WeekNumber(d));
current = current == 1 ? 2 : 1;
}
}

[Fact]
public void Invalid_anchor_week_throws()
{
var opts = Options.Create(new AcademicCalendarOptions
{
AnchorDate = new DateOnly(2026, 9, 1),
AnchorWeek = 3
});
Assert.Throws<InvalidOperationException>(() => new AcademicCalendar(opts));
}
}