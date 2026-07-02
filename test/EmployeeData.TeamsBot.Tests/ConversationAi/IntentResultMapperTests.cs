using EmployeeData.TeamsBot.Application.ConversationAi;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Tests.ConversationAi;

/// <summary>Maps an Azure OpenAI tool call (function name + raw JSON args) to an <see cref="IntentResult"/> (section 22.1).</summary>
public sealed class IntentResultMapperTests
{
    [Fact]
    public void Parameterless_intent_maps_name_only()
    {
        IntentResult result = IntentResultMapper.FromToolCall(IntentNames.GetMyHours, "{}");

        Assert.Equal(IntentNames.GetMyHours, result.Intent);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Add_motivation_maps_string_arguments()
    {
        const string argsJson =
            """{ "motivationType": "Annual Leave", "monthPhrase": "June", "description": "on leave", "targetEmployeeName": "Duminy, JD" }""";

        IntentResult result = IntentResultMapper.FromToolCall(IntentNames.AddMotivation, argsJson);

        Assert.Equal(IntentNames.AddMotivation, result.Intent);
        Assert.Equal("Annual Leave", result.Arguments["motivationType"]);
        Assert.Equal("June", result.Arguments["monthPhrase"]);
        Assert.Equal("Duminy, JD", result.Arguments["targetEmployeeName"]);
    }

    [Fact]
    public void Clarify_carries_reason()
    {
        IntentResult result = IntentResultMapper.FromToolCall(IntentNames.Clarify, """{ "reason": "ambiguous request" }""");

        Assert.Equal("ambiguous request", result.Arguments["reason"]);
    }

    [Fact]
    public void Malformed_arguments_yield_no_parameters()
    {
        IntentResult result = IntentResultMapper.FromToolCall(IntentNames.GetMyHours, "not json");

        Assert.Equal(IntentNames.GetMyHours, result.Intent);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Argument_lookup_is_case_insensitive()
    {
        IntentResult result = IntentResultMapper.FromToolCall(IntentNames.GetTeamHistory, """{ "months": "3" }""");

        Assert.Equal("3", result.Arguments["MONTHS"]);
    }
}
