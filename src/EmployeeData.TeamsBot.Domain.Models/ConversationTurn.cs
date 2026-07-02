namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>One prior turn of conversation context (role + text) for follow-up intent resolution (FR-4.2). No employee data.</summary>
public sealed record ConversationTurn(string Role, string Text);
