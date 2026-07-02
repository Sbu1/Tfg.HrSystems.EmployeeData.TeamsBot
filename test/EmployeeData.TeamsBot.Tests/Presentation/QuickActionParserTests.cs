using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Presentation.Bot;
using Newtonsoft.Json.Linq;

namespace EmployeeData.TeamsBot.Tests.Presentation;

/// <summary>Action.Submit taps carry `data.intent` and bypass the LLM (FR-4.2). Parse the activity value into an intent.</summary>
public sealed class QuickActionParserTests
{
    [Fact]
    public void Parses_intent_only_button()
    {
        object value = JObject.FromObject(new { intent = IntentNames.GetMyHours });

        Assert.True(QuickActionParser.TryParse(value, out IntentResult intent));
        Assert.Equal(IntentNames.GetMyHours, intent.Intent);
        Assert.Empty(intent.Arguments);
    }

    [Fact]
    public void Parses_intent_with_arguments()
    {
        object value = JObject.FromObject(new { intent = IntentNames.GetTeamHistory, months = "3" });

        Assert.True(QuickActionParser.TryParse(value, out IntentResult intent));
        Assert.Equal(IntentNames.GetTeamHistory, intent.Intent);
        Assert.Equal("3", intent.Arguments["months"]);
    }

    [Fact]
    public void Null_value_is_not_a_quick_action()
    {
        Assert.False(QuickActionParser.TryParse(null, out _));
    }

    [Fact]
    public void Value_without_intent_is_not_a_quick_action()
    {
        Assert.False(QuickActionParser.TryParse(JObject.FromObject(new { foo = "bar" }), out _));
    }
}
