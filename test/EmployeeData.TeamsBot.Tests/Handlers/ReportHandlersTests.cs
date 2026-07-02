using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;
using EmployeeData.TeamsBot.Tests.Domain;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-3.1/3.2 extension - a manager viewing one named direct report's standing / history.</summary>
public sealed class ReportHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 16, 6, 0, 0, TimeSpan.Zero);

    private static List<TeamMemberMonths> Team() =>
    [
        new("Sikhakhane, Sibusiso", 111, [new MonthlyHours(6, 2026, 100), new MonthlyHours(5, 2026, 61)]),
        new("Green, Leonard", 222, [new MonthlyHours(6, 2026, 40)])
    ];

    [Fact]
    public async Task Report_hours_returns_the_named_reports_standing()
    {
        var handler = new GetReportHoursHandler(
            new FakeEmployeeDataClient(team: Team()),
            new PaceCalculator(new StubWorkingDayCalendar(20, 10), goalHours: 100, earlyMonthThresholdPercent: 30, maxHoursPerWorkingDay: 10),
            new FixedTimeProvider(Now));

        TeamMemberStanding standing = await handler.HandleAsync(new GetReportHoursRequest(999, 111), null, CancellationToken.None);

        Assert.Equal(111, standing.EmployeeNumber);
        Assert.Equal(100, standing.Hours);
        Assert.Equal(PaceStatus.OnTrack, standing.Status);
    }

    [Fact]
    public async Task Report_history_returns_the_named_reports_months()
    {
        var handler = new GetReportHistoryHandler(new FakeEmployeeDataClient(team: Team()), goalHours: 100);

        TeamMemberHistory history = await handler.HandleAsync(new GetReportHistoryRequest(999, 111, 6), null, CancellationToken.None);

        Assert.Equal(111, history.EmployeeNumber);
        Assert.Equal(new[] { 6, 5 }, history.Months.Select(m => m.CalendarMonth));
        Assert.Equal(new[] { true, false }, history.Months.Select(m => m.GoalMet));
    }
}
