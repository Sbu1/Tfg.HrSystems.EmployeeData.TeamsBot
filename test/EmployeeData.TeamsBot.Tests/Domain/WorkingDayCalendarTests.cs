using EmployeeData.TeamsBot.Domain.Services;

namespace EmployeeData.TeamsBot.Tests.Domain;

public sealed class WorkingDayCalendarTests
{
    private readonly WorkingDayCalendar _calendar = new();

    [Fact]
    public void WorkingDaysInMonth_excludes_weekends()
    {
        // January 2026 has 31 days: 5 Saturdays + 4 Sundays = 9 weekend days, leaving 22 working days.
        Assert.Equal(22, _calendar.WorkingDaysInMonth(2026, 1));
    }

    [Theory]
    [InlineData(2, 2)]   // Thu 1 + Fri 2 Jan 2026
    [InlineData(4, 2)]   // + Sat 3, Sun 4 -> still 2
    [InlineData(5, 3)]   // + Mon 5 -> 3
    public void WorkingDaysElapsed_counts_weekdays_to_date(int dayOfMonth, int expected)
    {
        Assert.Equal(expected, _calendar.WorkingDaysElapsed(new DateOnly(2026, 1, dayOfMonth)));
    }
}
