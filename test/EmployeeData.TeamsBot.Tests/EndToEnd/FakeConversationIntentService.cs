using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Tests.EndToEnd;

/// <summary>Returns a preset intent so E2E turns don't call Azure OpenAI.</summary>
internal sealed class FakeConversationIntentService(IntentResult result) : IConversationIntentService
{
    public Task<IntentResult> ResolveIntentAsync(string userText, IReadOnlyList<ConversationTurn> history, CancellationToken ct) =>
        Task.FromResult(result);
}
