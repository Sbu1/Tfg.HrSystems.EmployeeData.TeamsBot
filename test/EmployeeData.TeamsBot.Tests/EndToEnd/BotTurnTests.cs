using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Presentation.Bot;
using EmployeeData.TeamsBot.Tests.Handlers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EmployeeData.TeamsBot.Tests.EndToEnd;

/// <summary>F9-S3 - end-to-end turns through the Bot Framework TestAdapter (message -> intent -> dispatch -> reply).</summary>
public sealed class BotTurnTests
{
    private static IntentResult Intent(string name, Dictionary<string, string>? args = null) =>
        new(name, args ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    // Data results reply as Adaptive Cards (no Text); fall back to the serialized card so content asserts hold.
    private static string ReplyContent(IActivity activity)
    {
        IMessageActivity message = activity.AsMessageActivity();
        if (!string.IsNullOrEmpty(message.Text))
        {
            return message.Text;
        }

        object? content = message.Attachments?.FirstOrDefault()?.Content;
        return content is null ? string.Empty : JsonConvert.SerializeObject(content);
    }

    private static async Task<string> ReplyAsync(IntentResult intent, int? employeeNumber, FakeEmployeeDataClient client)
    {
        EmployeeBot bot = TestBot.Build(intent, employeeNumber, client);
        TestAdapter adapter = TestBot.Adapter();
        string reply = string.Empty;

        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("hi")
            .AssertReply(activity => reply = ReplyContent(activity))
            .StartTestAsync();

        return reply;
    }

    [Fact]
    public async Task Employee_gets_their_standing()
    {
        var client = new FakeEmployeeDataClient(new EmployeeHours("Solo", [new MonthlyHours(6, 2026, 84)], []), team: []);

        string reply = await ReplyAsync(Intent(IntentNames.GetMyHours), employeeNumber: 1, client);

        Assert.Contains("84h", reply);
    }

    [Fact]
    public async Task Employee_is_declined_a_team_intent() // TS-04
    {
        var client = new FakeEmployeeDataClient(team: []); // empty team -> Employee role

        string reply = await ReplyAsync(Intent(IntentNames.GetAtRisk), employeeNumber: 1, client);

        Assert.Contains("manager", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_user_is_pointed_to_HR()
    {
        string reply = await ReplyAsync(Intent(IntentNames.GetMyHours), employeeNumber: null, new FakeEmployeeDataClient());

        Assert.Contains("HR", reply);
    }

    [Fact]
    public async Task Backend_failure_is_graceful() // TS-05 / BR-09
    {
        var client = new FakeEmployeeDataClient(team: []); // employee not configured -> GetEmployeeAsync throws

        string reply = await ReplyAsync(Intent(IntentNames.GetMyHours), employeeNumber: 1, client);

        Assert.Contains("unreachable", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Motivation_is_logged() // TS-03
    {
        var client = new FakeEmployeeDataClient(new EmployeeHours("Solo", [new MonthlyHours(5, 2026, 80)], []), team: [])
        {
            MotivationTypes = [new MotivationType(1, "Annual Leave")],
            AddResultId = 4
        };
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["monthPhrase"] = "202605", ["motivationType"] = "Annual Leave", ["description"] = "leave"
        };

        string reply = await ReplyAsync(Intent(IntentNames.AddMotivation, args), employeeNumber: 1, client);

        Assert.Contains("Logged", reply);
    }

    [Fact]
    public async Task Quick_action_button_bypasses_the_llm()
    {
        var client = new FakeEmployeeDataClient(new EmployeeHours("Solo", [new MonthlyHours(6, 2026, 84)], []), team: []);
        // The intent service would say "clarify" - the tapped button must win and run get_my_hours instead.
        EmployeeBot bot = TestBot.Build(Intent(IntentNames.Clarify), employeeNumber: 1, client);
        TestAdapter adapter = TestBot.Adapter();

        var tap = (Activity)MessageFactory.Text(string.Empty);
        tap.Value = JObject.FromObject(new { intent = IntentNames.GetMyHours });

        string reply = string.Empty;
        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send(tap)
            .AssertReply(activity => reply = ReplyContent(activity))
            .StartTestAsync();

        Assert.Contains("84h", reply);
    }
}
