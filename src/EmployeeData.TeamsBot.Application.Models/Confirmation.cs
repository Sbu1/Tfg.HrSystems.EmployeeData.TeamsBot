namespace EmployeeData.TeamsBot.Application.Models;

/// <summary>
/// A pending write awaiting the user's confirmation (FR-4.4). The Presentation layer renders <see cref="Prompt"/>
/// with a Confirm button carrying <see cref="ConfirmIntent"/> + the already-resolved <see cref="Arguments"/>, so
/// the follow-up tap commits. The commit path re-clamps those arguments - it never trusts the round-tripped values.
/// </summary>
public sealed record Confirmation(string Prompt, string ConfirmIntent, IReadOnlyDictionary<string, string> Arguments);
