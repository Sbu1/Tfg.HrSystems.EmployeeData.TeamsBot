using AdaptiveCards;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Models;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace EmployeeData.TeamsBot.Presentation.Bot;

/// <summary>
/// Builds Adaptive Card attachments for the bot (FR-4.2, section 13.4): a role-aware welcome card and a result
/// card per view, each with <c>Action.Submit</c> quick-action buttons that carry <c>data.intent</c> (parsed back
/// by <see cref="QuickActionParser"/>). Card bodies reuse <see cref="TurnResponseRenderer"/> for their text.
/// </summary>
public sealed class CardFactory(TurnResponseRenderer renderer)
{
    private static readonly AdaptiveSchemaVersion Schema = new(1, 4);

    public Attachment Welcome(CallerRole role)
    {
        var card = new AdaptiveCard(Schema);
        card.Body.Add(new AdaptiveTextBlock("In-Office Hours Assistant") { Size = AdaptiveTextSize.Large, Weight = AdaptiveTextWeight.Bolder });
        card.Body.Add(new AdaptiveTextBlock("What would you like to see?") { Wrap = true });
        AddActions(card, WelcomeActions(role));
        return Attach(card);
    }

    public Attachment ForResult(TurnResult result)
    {
        if (result.Payload is Confirmation confirmation)
        {
            return ConfirmCard(confirmation);
        }

        var card = new AdaptiveCard(Schema);
        card.Body.Add(new AdaptiveTextBlock(Title(result.Intent)) { Size = AdaptiveTextSize.Medium, Weight = AdaptiveTextWeight.Bolder });
        card.Body.Add(new AdaptiveTextBlock(renderer.Render(result)) { Wrap = true });
        AddActions(card, FollowUpActions(result.Intent));
        return Attach(card);
    }

    private static Attachment ConfirmCard(Confirmation confirmation)
    {
        var card = new AdaptiveCard(Schema);
        card.Body.Add(new AdaptiveTextBlock(confirmation.Prompt) { Wrap = true, Weight = AdaptiveTextWeight.Bolder });

        var confirmData = new Dictionary<string, object> { ["intent"] = confirmation.ConfirmIntent };
        foreach (KeyValuePair<string, string> argument in confirmation.Arguments)
        {
            confirmData[argument.Key] = argument.Value;
        }

        card.Actions.Add(new AdaptiveSubmitAction { Title = "Confirm", Data = confirmData });
        card.Actions.Add(new AdaptiveSubmitAction { Title = "Cancel", Data = new Dictionary<string, object> { ["intent"] = IntentNames.Help } });
        return Attach(card);
    }

    private static void AddActions(AdaptiveCard card, IEnumerable<(string Title, string Intent)> actions)
    {
        foreach ((string title, string intent) in actions)
        {
            card.Actions.Add(new AdaptiveSubmitAction
            {
                Title = title,
                Data = new Dictionary<string, object> { ["intent"] = intent }
            });
        }
    }

    private static IEnumerable<(string Title, string Intent)> WelcomeActions(CallerRole role)
    {
        yield return ("My hours", IntentNames.GetMyHours);
        yield return ("My history", IntentNames.GetMyHistory);
        yield return ("Peer standing", IntentNames.GetPeerStanding);
        if (role == CallerRole.Manager)
        {
            yield return ("Team this month", IntentNames.GetTeamThisMonth);
            yield return ("Who's at risk", IntentNames.GetAtRisk);
        }
    }

    private static (string Title, string Intent)[] FollowUpActions(string? intent) => intent switch
    {
        IntentNames.GetMyHours => [("My history", IntentNames.GetMyHistory), ("Peer standing", IntentNames.GetPeerStanding)],
        IntentNames.GetTeamThisMonth => [("Who's at risk", IntentNames.GetAtRisk)],
        _ => []
    };

    private static string Title(string? intent) => intent switch
    {
        IntentNames.GetMyHours => "Your hours this month",
        IntentNames.GetMyHistory => "Your recent history",
        IntentNames.GetPeerStanding => "Peer standing",
        IntentNames.GetTeamThisMonth => "Team this month",
        IntentNames.GetTeamHistory => "Team history",
        IntentNames.GetAtRisk => "At risk",
        IntentNames.GetReportHours => "Report - this month",
        IntentNames.GetReportHistory => "Report - history",
        IntentNames.ListMotivations => "Motivations",
        IntentNames.AddMotivation => "Motivation",
        _ => "Result"
    };

    private static Attachment Attach(AdaptiveCard card) => new()
    {
        ContentType = AdaptiveCard.ContentType,
        // Serialize to a JObject. Passing the live AdaptiveCard object as Content makes the Bot SDK's
        // AttachmentMemoryStreamConverter recurse infinitely (stack overflow) when the real CloudAdapter sends it.
        Content = JObject.Parse(card.ToJson())
    };
}
