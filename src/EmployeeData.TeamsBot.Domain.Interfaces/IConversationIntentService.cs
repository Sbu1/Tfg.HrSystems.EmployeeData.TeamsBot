using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Domain.Interfaces;

/// <summary>
/// Classifies a user message (with recent context) into an intent + parameters via the LLM (KA-03, section 22.1).
/// Only the user's text and function definitions are sent - never employee PII (TC-DC-02).
/// </summary>
public interface IConversationIntentService
{
    Task<IntentResult> ResolveIntentAsync(string userText, IReadOnlyList<ConversationTurn> history, CancellationToken ct);
}
