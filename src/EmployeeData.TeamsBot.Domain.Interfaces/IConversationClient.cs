namespace EmployeeData.TeamsBot.Domain.Interfaces;

/// <summary>
/// Phase-1 spike port: returns the model's reply to a single user message.
/// Evolves into KA-03 <c>IConversationIntentService.ResolveIntentAsync</c> (classifying the message into an
/// <c>IntentResult</c> via function calling - technical-spec §5 / §22.1) once intent handling lands.
/// </summary>
public interface IConversationClient
{
    Task<string> GetReplyAsync(string userText, CancellationToken ct);
}
