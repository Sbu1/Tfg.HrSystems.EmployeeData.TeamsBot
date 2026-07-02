namespace EmployeeData.TeamsBot.Application.Models;

/// <summary>
/// The outcome of a dispatched turn, for the Presentation layer to render (A-DC-03): either a plain
/// <see cref="Message"/> (help/clarify/declined/acknowledgements) or an <see cref="Intent"/> + typed
/// <see cref="Payload"/> that a card factory renders. Exactly one shape is set.
/// </summary>
public sealed record TurnResult(string? Message, string? Intent, object? Payload)
{
    public static TurnResult Text(string message) => new(message, null, null);

    public static TurnResult Data(string intent, object payload) => new(null, intent, payload);
}
