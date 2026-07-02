using EmployeeData.TeamsBot.Application.Graph;
using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace EmployeeData.TeamsBot.Presentation.Bot;

/// <summary>
/// The Teams turn pipeline (FR-4.x): resolve the caller's identity + role, classify the message into an intent
/// (or take it straight from a tapped quick-action button, bypassing the LLM), dispatch to the right handler,
/// and reply with an Adaptive Card. Unmapped users get the FR-4.3 "point to HR" message; any failure degrades to
/// a BR-09 "service unreachable" reply.
/// </summary>
public sealed class EmployeeBot(
    CallerIdentityResolver identity,
    IConversationIntentService intentService,
    ConversationDispatcher dispatcher,
    TurnResponseRenderer renderer,
    CardFactory cards) : ActivityHandler
{
    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        try
        {
            string aadObjectId = turnContext.Activity.From?.AadObjectId ?? string.Empty;
            CallerIdentity? caller = aadObjectId.Length == 0
                ? null
                : await identity.ResolveAsync(aadObjectId, cancellationToken);

            if (caller is null)
            {
                // FR-4.3: unmapped user - explain and point to HR, no data, no lookup.
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("I couldn't find your employee record - please contact HR to get set up."),
                    cancellationToken);
                return;
            }

            // A tapped quick-action button carries the intent in Activity.Value and bypasses the LLM (FR-4.2).
            IntentResult intent = QuickActionParser.TryParse(turnContext.Activity.Value, out IntentResult quick)
                ? quick
                : await intentService.ResolveIntentAsync(turnContext.Activity.Text ?? string.Empty, [], cancellationToken);

            TurnResult result = await dispatcher.DispatchAsync(intent, caller, cancellationToken);
            await turnContext.SendActivityAsync(BuildReply(intent, result, caller.Role), cancellationToken);
        }
        catch (Exception)
        {
            // BR-09: never surface a raw error. Per-failure-mode handling + logging are refined in F8-S1/S2.
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Sorry, that service is unreachable right now - please try again shortly."),
                cancellationToken);
        }
    }

    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext,
        CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Attachment(cards.Welcome(CallerRole.Employee)), cancellationToken);
            }
        }
    }

    private IActivity BuildReply(IntentResult intent, TurnResult result, CallerRole role)
    {
        // Help shows the role-aware welcome card; data results render as cards; everything else is text.
        if (intent.Intent == IntentNames.Help)
        {
            return MessageFactory.Attachment(cards.Welcome(role));
        }

        // Keep a text summary alongside the card so plain channels and assertions still see the content.
        return result.Message is not null
            ? MessageFactory.Text(result.Message)
            : MessageFactory.Attachment(cards.ForResult(result));
    }
}
