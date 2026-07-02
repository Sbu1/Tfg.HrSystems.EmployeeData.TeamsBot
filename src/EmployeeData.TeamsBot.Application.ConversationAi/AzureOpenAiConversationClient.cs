using System.ClientModel;
using Azure.AI.OpenAI;
using EmployeeData.TeamsBot.Domain.Interfaces;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace EmployeeData.TeamsBot.Application.ConversationAi;

public sealed class AzureOpenAiConversationClient : IConversationClient
{
    private readonly ChatClient _chat;

    public AzureOpenAiConversationClient(IOptions<ConversationAiOptions> options)
    {
        var settings = options.Value;
        var client = new AzureOpenAIClient(new Uri(settings.Endpoint), new ApiKeyCredential(settings.ApiKey));
        _chat = client.GetChatClient(settings.Deployment);
    }

    public async Task<string> GetReplyAsync(string userText, CancellationToken ct)
    {
        // GPT-5-series param rules (technical-spec §13.3): it rejects a non-default Temperature and rejects
        // max_tokens (requires max_completion_tokens). Azure.AI.OpenAI 2.1.0's default api-version still
        // serialises MaxOutputTokenCount as max_tokens, so for this spike we send no options at all - no token
        // cap, no temperature. Follow-up: pin a newer api-version that emits max_completion_tokens.
        ChatCompletion completion = await _chat.CompleteChatAsync(
            [ChatMessage.CreateUserMessage(userText)], cancellationToken: ct);

        return string.Concat(completion.Content.Select(part => part.Text));
    }
}
