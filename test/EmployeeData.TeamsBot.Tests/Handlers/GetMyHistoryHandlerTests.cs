using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-1.2 - recent monthly history: hours + goal-met (>=100), most recent first, capped at 6 (EC-02).</summary>
public sealed class GetMyHistoryHandlerTests
{
    private static GetMyHistoryHandler Build(EmployeeHours employee) =>
        new(new FakeEmployeeDataClient(employee), goalHours: 100);

    [Fact]
    public async Task Maps_goal_met_flag_and_orders_most_recent_first()
    {
        var employee = new EmployeeHours(
            "Du Toit, Werner",
            [new MonthlyHours(5, 2026, 116), new MonthlyHours(6, 2026, 84), new MonthlyHours(4, 2026, 95)],
            []);

        IReadOnlyList<MonthHours> history =
            await Build(employee).HandleAsync(new GetMyHistoryRequest(123456), null, CancellationToken.None);

        Assert.Equal(new[] { 6, 5, 4 }, history.Select(m => m.CalendarMonth));
        Assert.Equal(new[] { false, true, false }, history.Select(m => m.GoalMet));
    }

    [Fact]
    public async Task Caps_at_six_months()
    {
        var months = Enumerable.Range(1, 7).Select(m => new MonthlyHours(m, 2026, 100)).ToList();
        var employee = new EmployeeHours("Solo", months, []);

        IReadOnlyList<MonthHours> history =
            await Build(employee).HandleAsync(new GetMyHistoryRequest(1), null, CancellationToken.None);

        Assert.Equal(6, history.Count);
    }
}
