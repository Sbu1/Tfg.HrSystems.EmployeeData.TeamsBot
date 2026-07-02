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
        "You classify a TFG staff member's Microsoft Teams message into exactly one function. Call the single " +
        "most appropriate function. Never put employee numbers or ids in arguments - use names and phrases as " +
        "the user said them. If the message is ambiguous or unsupported, call clarify with a short reason. Use " +
        "help for greetings or 'what can you do' questions.";

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
