using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;
using EmployeeData.TeamsBot.Tests.Domain;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-2.1 - AddMotivation: BR-05 qualifying-month pre-check + BR-06 upsert detection.</summary>
public sealed class AddMotivationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 16, 6, 0, 0, TimeSpan.Zero);

    private static AddMotivationHandler Build(FakeEmployeeDataClient client) =>
        new(client,
            new PaceCalculator(new StubWorkingDayCalendar(20, 10), goalHours: 100, earlyMonthThresholdPercent: 30, maxHoursPerWorkingDay: 10),
            new FixedTimeProvider(Now));

    private static EmployeeHours SelfWithMonth(int year, int month, int hours) =>
        new("Solo", [new MonthlyHours(month, year, hours)], []);

    [Fact]
    public async Task Past_month_below_goal_posts_and_is_not_an_update()
    {
        var client = new FakeEmployeeDataClient(SelfWithMonth(2026, 5, 80)) { AddResultId = 4 };

        AddMotivationResult result = await Build(client)
            .HandleAsync(new AddMotivationRequest(1, 1, MotivationTypeId: 1, CalendarMonth: "202605", Description: "leave"), null, CancellationToken.None);

        Assert.Equal(4, result.Id);
        Assert.False(result.Updated);
        Assert.Equal("202605", client.LastAdded!.CalendarMonth);
        Assert.Equal(1, client.LastAdded!.MotivationTypeId);
    }

    [Fact]
    public async Task Past_month_that_met_goal_is_declined_before_posting()
    {
        var client = new FakeEmployeeDataClient(SelfWithMonth(2026, 5, 116));

        await Assert.ThrowsAsync<MotivationNotAllowedException>(() => Build(client)
            .HandleAsync(new AddMotivationRequest(1, 1, 1, "202605", "leave"), null, CancellationToken.None));

        Assert.Null(client.LastAdded);
    }

    [Fact]
    public async Task Existing_same_type_and_month_is_an_update()
    {
        var existing = new MotivationView(4, "202605", 1, "Annual Leave", "x", DateTimeOffset.UnixEpoch);
        var client = new FakeEmployeeDataClient(SelfWithMonth(2026, 5, 80)) { Motivations = [existing], AddResultId = 4 };

        AddMotivationResult result = await Build(client)
            .HandleAsync(new AddMotivationRequest(1, 1, 1, "202605", "updated"), null, CancellationToken.None);

        Assert.True(result.Updated);
    }

    [Fact]
    public async Task Current_month_behind_pace_qualifies()
    {
        var client = new FakeEmployeeDataClient(SelfWithMonth(2026, 6, 40)); // below pace at 10/20 -> Behind

        AddMotivationResult result = await Build(client)
            .HandleAsync(new AddMotivationRequest(1, 1, 1, "202606", "behind"), null, CancellationToken.None);

        Assert.Equal(1, result.Id);
    }
}
