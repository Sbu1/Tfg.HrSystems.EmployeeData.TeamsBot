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
            new GetReportHoursHandler(client, pace, time),
            new GetReportHistoryHandler(client),
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

    [Fact]
    public async Task Add_motivation_asks_to_confirm_before_committing() // FR-4.4
    {
        var client = new FakeEmployeeDataClient(new EmployeeHours("Solo", [new MonthlyHours(5, 2026, 80)], []), team: [])
        {
            MotivationTypes = [new MotivationType(1, "Annual Leave")]
        };
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["monthPhrase"] = "202605", ["motivationType"] = "Annual Leave", ["description"] = "leave"
        };

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.AddMotivation, args), Employee, CancellationToken.None);

        var confirmation = Assert.IsType<Confirmation>(result.Payload);
        Assert.Equal(IntentNames.AddMotivationConfirmed, confirmation.ConfirmIntent);
        Assert.Null(client.LastAdded); // nothing committed until confirmed
    }

    [Fact]
    public async Task Confirmed_add_commits()
    {
        var client = new FakeEmployeeDataClient(new EmployeeHours("Solo", [new MonthlyHours(5, 2026, 80)], []), team: []) { AddResultId = 4 };
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["motivationTypeId"] = "1", ["calendarMonth"] = "202605", ["targetEmployeeNumber"] = "1", ["description"] = "leave"
        };

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.AddMotivationConfirmed, args), Employee, CancellationToken.None);

        Assert.Equal("202605", client.LastAdded!.CalendarMonth);
        Assert.IsType<AddMotivationResult>(result.Payload);
    }

    [Fact]
    public async Task Confirmed_add_rejects_a_tampered_target() // TR-03 re-clamp
    {
        var client = new FakeEmployeeDataClient(new EmployeeHours("Solo", [new MonthlyHours(5, 2026, 80)], []), team: []);
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["motivationTypeId"] = "1", ["calendarMonth"] = "202605", ["targetEmployeeNumber"] = "999", ["description"] = "x"
        };

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.AddMotivationConfirmed, args), Employee, CancellationToken.None);

        Assert.NotNull(result.Message); // declined
        Assert.Null(client.LastAdded);
    }

    [Fact]
    public async Task Remove_motivation_asks_to_confirm() // FR-4.4
    {
        var owned = new MotivationView(4, "202605", 1, "Annual Leave", "x", DateTimeOffset.UnixEpoch);
        var client = new FakeEmployeeDataClient(team: []) { Motivations = [owned] };
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["selector"] = "annual leave" };

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.RemoveMotivation, args), Employee, CancellationToken.None);

        var confirmation = Assert.IsType<Confirmation>(result.Payload);
        Assert.Equal(IntentNames.RemoveMotivationConfirmed, confirmation.ConfirmIntent);
        Assert.Equal("4", confirmation.Arguments["motivationId"]);
        Assert.Null(client.LastDeletedId);
    }

    [Fact]
    public async Task Report_hours_resolves_a_named_report_for_a_manager()
    {
        var client = new FakeEmployeeDataClient(team:
            [new TeamMemberMonths("Sikhakhane, Sibusiso", 111, [new MonthlyHours(6, 2026, 100)])]);
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["targetEmployeeName"] = "Sibusiso" };

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.GetReportHours, args), Manager, CancellationToken.None);

        var standing = Assert.IsType<TeamMemberStanding>(result.Payload);
        Assert.Equal(111, standing.EmployeeNumber);
    }

    [Fact]
    public async Task Report_hours_unknown_name_asks_to_clarify()
    {
        var client = new FakeEmployeeDataClient(team:
            [new TeamMemberMonths("Sikhakhane, Sibusiso", 111, [new MonthlyHours(6, 2026, 100)])]);
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["targetEmployeeName"] = "Nobody" };

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.GetReportHours, args), Manager, CancellationToken.None);

        Assert.NotNull(result.Message);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Report_hours_is_declined_for_an_employee()
    {
        var client = new FakeEmployeeDataClient(team: []);
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["targetEmployeeName"] = "Sibusiso" };

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.GetReportHours, args), Employee, CancellationToken.None);

        Assert.NotNull(result.Message);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Confirmed_remove_deletes()
    {
        var owned = new MotivationView(4, "202605", 1, "Annual Leave", "x", DateTimeOffset.UnixEpoch);
        var client = new FakeEmployeeDataClient(team: []) { Motivations = [owned], DeleteResult = true };
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["motivationId"] = "4" };

        TurnResult result = await Build(client).DispatchAsync(Intent(IntentNames.RemoveMotivationConfirmed, args), Employee, CancellationToken.None);

        Assert.Equal(4, client.LastDeletedId);
        Assert.NotNull(result.Message);
    }
}
