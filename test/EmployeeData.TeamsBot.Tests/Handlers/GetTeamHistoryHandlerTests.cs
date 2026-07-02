using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-3.2 - per direct report: hours/month over N months + goal-met, most recent first.</summary>
public sealed class GetTeamHistoryHandlerTests
{
    [Fact]
    public async Task Maps_each_report_history_with_goal_met_recent_first()
    {
        List<TeamMemberMonths> team =
        [
            new("Duminy, JD", 1111111, [new MonthlyHours(5, 2026, 116), new MonthlyHours(6, 2026, 84)])
        ];
        var handler = new GetTeamHistoryHandler(new FakeEmployeeDataClient(team: team), goalHours: 100);

        IReadOnlyList<TeamMemberHistory> history =
            await handler.HandleAsync(new GetTeamHistoryRequest(3333333), null, CancellationToken.None);

        TeamMemberHistory jd = Assert.Single(history);
        Assert.Equal(new[] { 6, 5 }, jd.Months.Select(m => m.CalendarMonth));
        Assert.Equal(new[] { false, true }, jd.Months.Select(m => m.GoalMet));
    }
}
