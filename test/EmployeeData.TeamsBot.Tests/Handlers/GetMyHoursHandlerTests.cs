using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;
using EmployeeData.TeamsBot.Tests.Domain;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-1.1 - the caller's current-month standing: MTD, gap to goal, pace status, "as at" (T-1).</summary>
public sealed class GetMyHoursHandlerTests
{
    private static GetMyHoursHandler Build(EmployeeHours employee, int totalWorkingDays, int elapsedWorkingDays, DateTimeOffset utcNow)
    {
        var pace = new PaceCalculator(
            new StubWorkingDayCalendar(totalWorkingDays, elapsedWorkingDays),
            goalHours: 100, earlyMonthThresholdPercent: 30, maxHoursPerWorkingDay: 10);
        return new GetMyHoursHandler(new FakeEmployeeDataClient(employee), pace, new FixedTimeProvider(utcNow));
    }

    [Fact]
    public async Task Builds_standing_with_gap_pace_and_as_at()
    {
        var employee = new EmployeeHours(
            "Du Toit, Werner",
            [new MonthlyHours(6, 2026, 84), new MonthlyHours(5, 2026, 116)],
            [new PeerHours("player1", 43)]);

        GetMyHoursHandler handler = Build(employee, totalWorkingDays: 20, elapsedWorkingDays: 10,
            utcNow: new DateTimeOffset(2026, 6, 16, 6, 0, 0, TimeSpan.Zero));

        EmployeeStanding standing = await handler.HandleAsync(new GetMyHoursRequest(123456), null, CancellationToken.None);

        Assert.Equal(84, standing.MtdHours);
        Assert.Equal(100, standing.GoalHours);
        Assert.Equal(16, standing.GapHours);           // 100 - 84
        Assert.Equal(PaceStatus.OnTrack, standing.Status); // 84 >= expected 50 at halfway
        Assert.Equal(new DateOnly(2026, 6, 15), standing.AsAt); // T-1
    }

    [Fact]
    public async Task Gap_is_zero_when_goal_already_met()
    {
        var employee = new EmployeeHours("Solo", [new MonthlyHours(6, 2026, 110)], []);

        GetMyHoursHandler handler = Build(employee, totalWorkingDays: 20, elapsedWorkingDays: 15,
            utcNow: new DateTimeOffset(2026, 6, 20, 6, 0, 0, TimeSpan.Zero));

        EmployeeStanding standing = await handler.HandleAsync(new GetMyHoursRequest(1), null, CancellationToken.None);

        Assert.Equal(0, standing.GapHours);
    }
}
