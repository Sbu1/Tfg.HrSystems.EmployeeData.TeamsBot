using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;
using EmployeeData.TeamsBot.Tests.Domain;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-3.3 - reports projected to miss the goal (BR-01), sorted by largest shortfall; on-track excluded.</summary>
public sealed class GetAtRiskHandlerTests
{
    private static GetAtRiskHandler Build(IReadOnlyList<TeamMemberMonths> team) =>
        new(new FakeEmployeeDataClient(team: team),
            new PaceCalculator(new StubWorkingDayCalendar(20, 10), goalHours: 100, earlyMonthThresholdPercent: 30, maxHoursPerWorkingDay: 10),
            new FixedTimeProvider(new DateTimeOffset(2026, 6, 16, 6, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task Lists_projected_to_miss_sorted_by_shortfall_and_excludes_on_track()
    {
        List<TeamMemberMonths> team =
        [
            new("Du Toit, Werner", 1234567, [new MonthlyHours(6, 2026, 84)]), // OnTrack -> excluded
            new("Duminy, JD", 1111111, [new MonthlyHours(6, 2026, 40)]),      // projected 80, shortfall 20
            new("Amla, Hash", 2222222, [new MonthlyHours(6, 2026, 20)])       // projected 40, shortfall 60
        ];

        IReadOnlyList<TeamMemberStanding> atRisk =
            await Build(team).HandleAsync(new GetAtRiskRequest(3333333), null, CancellationToken.None);

        Assert.Equal(new[] { 2222222, 1111111 }, atRisk.Select(s => s.EmployeeNumber));
        Assert.DoesNotContain(atRisk, s => s.EmployeeNumber == 1234567);
    }
}
