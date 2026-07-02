using System.ClientModel;
using Azure.AI.OpenAI;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace EmployeeData.TeamsBot.Application.ConversationAi;

/// <summary>
/// Classifies a message into an intent via <c>gpt-5.4-mini</c> function calling (KA-03, section 13.3/22.1).
/// Only the user's text, recent context, and function definitions are sent - never employee PII (TC-DC-02).
/// </summary>
public sealed class AzureOpenAiConversationIntentService : IConversationIntentService
{
    private const string SystemPrompt =
        "You route a TFG staff member's Microsoft Teams message to exactly one function - always call one. Pick " +
        "the single best-matching function even when the wording is loose; only call clarify when the message " +
        "genuinely matches no function. Treat 'my team', 'my employees', 'my staff', 'my reports' as the caller's " +
        "team. Routing guide: own hours / how am I doing this month -> get_my_hours; my past months / history -> " +
        "get_my_history; compare to peers / leaderboard / ranking -> get_peer_standing; team or employees standing " +
        "now / 'team hours' / how is my team doing -> get_team_this_month; team over past months / 'last month(s) " +
        "team' / team history -> get_team_history (set months to how many months back); who is behind or at risk " +
        "of missing the goal -> get_at_risk; ONE named report's hours this month -> get_report_hours (set " +
        "targetEmployeeName); a named report's history / 'X's hours for the past N months' -> get_report_history " +
        "(set targetEmployeeName and months); log or record a motivation -> add_motivation; remove or delete a " +
        "motivation -> remove_motivation; a greeting or 'what can you do' -> help. Never put employee numbers or " +
        "ids in arguments - use the names and phrases the user said.";

    private readonly ChatClient _chat;
    private readonly IReadOnlyList<ChatTool> _tools;

    public AzureOpenAiConversationIntentService(IOptions<ConversationAiOptions> options)
    {
        ConversationAiOptions settings = options.Value;
        var client = new AzureOpenAIClient(new Uri(settings.Endpoint), new ApiKeyCredential(settings.ApiKey));
        _chat = client.GetChatClient(settings.Deployment);
        _tools = ConversationIntentTools.Build();
    }

    public async Task<IntentResult> ResolveIntentAsync(
        string userText, IReadOnlyList<ConversationTurn> history, CancellationToken ct)
    {
        var messages = new List<ChatMessage> { new SystemChatMessage(SystemPrompt) };
        foreach (ConversationTurn turn in history)
        {
            messages.Add(IsAssistant(turn.Role) ? new AssistantChatMessage(turn.Text) : new UserChatMessage(turn.Text));
        }
        messages.Add(new UserChatMessage(userText));

        // GPT-5-series param rules (section 13.3): no Temperature and no max_tokens - leave both unset.
        var chatOptions = new ChatCompletionOptions { ToolChoice = ChatToolChoice.CreateAutoChoice() };
        foreach (ChatTool tool in _tools)
        {
            chatOptions.Tools.Add(tool);
        }

        ChatCompletion completion = await _chat.CompleteChatAsync(messages, chatOptions, ct);

        if (completion.ToolCalls.Count > 0)
        {
            ChatToolCall call = completion.ToolCalls[0];
            return IntentResultMapper.FromToolCall(call.FunctionName, call.FunctionArguments.ToString());
        }

        // The model replied in prose instead of choosing a function - treat as needing clarification.
        return new IntentResult(
            IntentNames.Clarify,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["reason"] = "no_intent_selected" });
    }

    private static bool IsAssistant(string role) =>
        role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("bot", StringComparison.OrdinalIgnoreCase);
}
