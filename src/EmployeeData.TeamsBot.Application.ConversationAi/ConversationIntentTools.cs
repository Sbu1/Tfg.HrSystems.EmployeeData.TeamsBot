using EmployeeData.TeamsBot.Domain.Constants;
using OpenAI.Chat;

namespace EmployeeData.TeamsBot.Application.ConversationAi;

/// <summary>The Azure OpenAI function (tool) set the model chooses from - exactly one per intent in section 22.1.</summary>
public static class ConversationIntentTools
{
    private const string NoParameters = """{"type":"object","properties":{},"required":[]}""";

    private static readonly IReadOnlyList<IntentToolDefinition> Definitions =
    [
        new(IntentNames.GetMyHours, "The user asks how they are tracking or their in-office hours this month.", NoParameters),
        new(IntentNames.GetMyHistory, "The user asks for their recent monthly hours history.", NoParameters),
        new(IntentNames.GetPeerStanding, "The user asks how they compare to their anonymised peers.", NoParameters),
        new(IntentNames.ListMotivations, "The user wants to see logged motivations.",
            """{"type":"object","properties":{"targetEmployeeName":{"type":"string","description":"A direct report's name; only when a manager asks about a report, otherwise omit."}},"required":[]}"""),
        new(IntentNames.AddMotivation, "The user wants to log a motivation for a month that missed the 100h goal.",
            """{"type":"object","properties":{"motivationType":{"type":"string","description":"The leave/absence type as named by the user."},"monthPhrase":{"type":"string","description":"The month exactly as the user said it, e.g. 'June' or 'last month'."},"description":{"type":"string"},"targetEmployeeName":{"type":"string","description":"A direct report's name when a manager logs on their behalf."}},"required":["motivationType","monthPhrase","description"]}"""),
        new(IntentNames.RemoveMotivation, "The user wants to remove a logged motivation.",
            """{"type":"object","properties":{"selector":{"type":"string","description":"How the user identified it, e.g. 'the June annual leave one'."}},"required":["selector"]}"""),
        new(IntentNames.GetTeamThisMonth, "A manager asks about their team's or employees' standing or hours this month (e.g. 'check hours for my employees', 'how is my team doing', 'team hours').", NoParameters),
        new(IntentNames.GetTeamHistory, "A manager asks about their team's hours over past months (e.g. 'team history', \"last month's team hours\", 'team hours over the last 3 months').",
            """{"type":"object","properties":{"months":{"type":"string","description":"How many months back; 1 for 'last month', defaults to 6 when omitted."}},"required":[]}"""),
        new(IntentNames.GetAtRisk, "A manager asks who on their team is behind or at risk of missing the goal.", NoParameters),
        new(IntentNames.Help, "The user greets the bot or asks what it can do.", NoParameters),
        new(IntentNames.Clarify, "The request is ambiguous or unsupported; ask the user to clarify.",
            """{"type":"object","properties":{"reason":{"type":"string"}},"required":["reason"]}""")
    ];

    public static IReadOnlyList<string> DefinedIntentNames => Definitions.Select(definition => definition.Name).ToList();

    public static IReadOnlyList<ChatTool> Build() =>
        Definitions
            .Select(definition => ChatTool.CreateFunctionTool(
                definition.Name, definition.Description, BinaryData.FromString(definition.ParametersSchemaJson)))
            .ToList();

    private sealed record IntentToolDefinition(string Name, string Description, string ParametersSchemaJson);
}
