using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;
using EmployeeData.TeamsBot.Tests.Domain;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-3.1 - per direct report: current-month hours + pace status (BR-01).</summary>
public sealed class GetTeamThisMonthHandlerTests
{
    private static GetTeamThisMonthHandler Build(IReadOnlyList<TeamMemberMonths> team) =>
        new(new FakeEmployeeDataClient(team: team),
            new PaceCalculator(new StubWorkingDayCalendar(20, 10), goalHours: 100, earlyMonthThresholdPercent: 30, maxHoursPerWorkingDay: 10),
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 16, 6, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task Maps_each_report_to_a_standing_with_pace()
    {
        List<TeamMemberMonths> team =
        [
            new("Du Toit, Werner", 1234567, [new MonthlyHours(6, 2026, 84)]),
            new("Duminy, JD", 1111111, [new MonthlyHours(6, 2026, 40)])
        ];

        IReadOnlyList<TeamMemberStanding> standings =
            await Build(team).HandleAsync(new GetTeamThisMonthRequest(3333333), null, CancellationToken.None);

        Assert.Equal(2, standings.Count);

        TeamMemberStanding werner = standings.Single(s => s.EmployeeNumber == 1234567);
        Assert.Equal(PaceStatus.OnTrack, werner.Status);   // 84 >= expected 50
        Assert.Equal(0, werner.ProjectedShortfall);

        TeamMemberStanding jd = standings.Single(s => s.EmployeeNumber == 1111111);
        Assert.Equal(PaceStatus.Behind, jd.Status);        // 40 < 50 but recoverable
        Assert.Equal(20, jd.ProjectedShortfall);           // 100 - projected 80
    }
}
