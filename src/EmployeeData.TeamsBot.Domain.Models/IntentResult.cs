namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>
/// A classified user intent (spec section 22.1): the function/intent name plus its raw string arguments as the
/// model emitted them. Arguments are names/strings only - never ids or employee numbers - which the dispatcher
/// later resolves to concrete values under the clamp (DC-DC-03).
/// </summary>
public sealed record IntentResult(string Intent, IReadOnlyDictionary<string, string> Arguments);
