using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;
using EmployeeData.TeamsBot.Tests.Domain;
using EmployeeData.TeamsBot.Tests.Handlers;

namespace EmployeeData.TeamsBot.Tests.Dispatch;

/// <summary>F4-S2 - route an IntentResult to the right handler, gate by role, resolve params, return a TurnResult.</summary>
public sealed class ConversationDispatcherTests
{
    private static ConversationDispatcher Build(FakeEmployeeDataClient client)
    {
        var pace = new PaceCalculator(new StubWorkingDayCalendar(20, 10), goalHours: 100, earlyMonthThresholdPercent: 30, maxHoursPerWorkingDay: 10);
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 6, 16, 6, 0, 0, TimeSpan.Zero));
        return new ConversationDispatcher(
            new GetMyHoursHandler(client, pace, time),
            new GetMyHistoryHandler(client),
            new GetPeerStandingHandler(client, new LeaderboardBuilder()),
            new GetMotivationTypesHandler(client),
            new ListMotivationsHandler(client),
            new AddMotivationHandler(client, pace, time),
            new RemoveMotivationHandler(client),
            new GetTeamThisMonthHandler(client, pace, time),
            new GetTeamHistoryHandler(client),
            new GetAtRiskHandler(client, pace, time),
            client, time);
    }

    private static IntentResult Intent(string name, Dictionary<string, string>? args = null) =>
        new(name, args ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    private static readonly CallerIdentity Employee = new(1, CallerRole.Employee);
    private static readonly CallerIdentity Manager = new(3333333, CallerRole.Manager);

    [Fact]
    public async Task Get_my_hours_returns_standing_payload()
    {
        var client = new FakeEmployeeDataClient(new EmployeeHours("Solo", [new MonthlyHours(6, 2026, 84)], []));

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.GetMyHours), Employee, CancellationToken.None);

        Assert.Equal(IntentNames.GetMyHours, result.Intent);
        Assert.IsType<EmployeeStanding>(result.Payload);
    }

    [Fact]
    public async Task Team_intent_is_declined_for_an_employee()
    {
        var client = new FakeEmployeeDataClient();

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.GetAtRisk), Employee, CancellationToken.None);

        Assert.NotNull(result.Message);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Team_intent_returns_data_for_a_manager()
    {
        var client = new FakeEmployeeDataClient(team: [new TeamMemberMonths("Report", 1111111, [new MonthlyHours(6, 2026, 40)])]);

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.GetTeamThisMonth), Manager, CancellationToken.None);

        Assert.Equal(IntentNames.GetTeamThisMonth, result.Intent);
        Assert.IsAssignableFrom<IReadOnlyList<TeamMemberStanding>>(result.Payload);
    }

    [Fact]
    public async Task Help_and_clarify_return_text()
    {
        var client = new FakeEmployeeDataClient();
        ConversationDispatcher dispatcher = Build(client);

        Assert.NotNull((await dispatcher.DispatchAsync(Intent(IntentNames.Help), Employee, CancellationToken.None)).Message);
        Assert.NotNull((await dispatcher.DispatchAsync(Intent(IntentNames.Clarify, new() { ["reason"] = "huh" }), Employee, CancellationToken.None)).Message);
    }

    [Fact]
    public async Task Add_motivation_with_an_unresolvable_month_asks_to_clarify_and_does_not_post()
    {
        var client = new FakeEmployeeDataClient(new EmployeeHours("Solo", [new MonthlyHours(5, 2026, 80)], []))
        {
            MotivationTypes = [new MotivationType(1, "Annual Leave")]
        };

        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["monthPhrase"] = "banana", ["motivationType"] = "Annual Leave", ["description"] = "x"
        };
        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.AddMotivation, args), Employee, CancellationToken.None);

        Assert.NotNull(result.Message);
        Assert.Null(client.LastAdded);
    }
}
