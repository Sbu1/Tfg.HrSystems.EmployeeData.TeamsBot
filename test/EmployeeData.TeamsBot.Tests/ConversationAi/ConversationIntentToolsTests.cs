using EmployeeData.TeamsBot.Application.ConversationAi;
using EmployeeData.TeamsBot.Domain.Constants;

namespace EmployeeData.TeamsBot.Tests.ConversationAi;

/// <summary>The AOAI function/tool set must cover exactly the intents in section 22.1 - no drift.</summary>
public sealed class ConversationIntentToolsTests
{
    [Fact]
    public void Defines_every_intent_in_the_reference_table()
    {
        string[] expected =
        [
            IntentNames.GetMyHours, IntentNames.GetMyHistory, IntentNames.GetPeerStanding,
            IntentNames.ListMotivations, IntentNames.AddMotivation, IntentNames.RemoveMotivation,
            IntentNames.GetTeamThisMonth, IntentNames.GetTeamHistory, IntentNames.GetAtRisk,
            IntentNames.Help, IntentNames.Clarify
        ];

        Assert.Equal(expected.OrderBy(name => name), ConversationIntentTools.DefinedIntentNames.OrderBy(name => name));
    }

    [Fact]
    public void Builds_one_chat_tool_per_intent()
    {
        Assert.Equal(11, ConversationIntentTools.Build().Count);
    }
}
