using EmployeeData.TeamsBot.Application.Graph;
using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;
using EmployeeData.TeamsBot.Presentation.Bot;
using EmployeeData.TeamsBot.Tests.Domain;
using EmployeeData.TeamsBot.Tests.Handlers;
using EmployeeData.TeamsBot.Tests.Identity;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;

namespace EmployeeData.TeamsBot.Tests.EndToEnd;

/// <summary>Composes a real <see cref="EmployeeBot"/> over fakes (no Graph/AOAI/network) for TestAdapter E2E turns.</summary>
internal static class TestBot
{
    public static EmployeeBot Build(IntentResult intent, int? employeeNumber, FakeEmployeeDataClient client)
    {
        var pace = new PaceCalculator(new StubWorkingDayCalendar(20, 10), goalHours: 100, earlyMonthThresholdPercent: 30, maxHoursPerWorkingDay: 10);
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 6, 16, 6, 0, 0, TimeSpan.Zero));

        var dispatcher = new ConversationDispatcher(
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

        var resolver = new CallerIdentityResolver(new FakeEmployeeDirectory(employeeNumber), client);
        var renderer = new TurnResponseRenderer();
        return new EmployeeBot(resolver, new FakeConversationIntentService(intent), dispatcher, renderer, new CardFactory(renderer));
    }

    public static TestAdapter Adapter() => new(new ConversationReference
    {
        ChannelId = "test",
        ServiceUrl = "https://test",
        User = new ChannelAccount("user") { AadObjectId = "aad-1" },
        Bot = new ChannelAccount("bot"),
        Conversation = new ConversationAccount(id: "c1")
    });
}
