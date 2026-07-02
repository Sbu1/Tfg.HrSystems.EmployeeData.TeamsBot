using EmployeeData.TeamsBot.Application.Graph;
using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace EmployeeData.TeamsBot.Presentation.Bot;

/// <summary>
/// The Teams turn pipeline (FR-4.x): resolve the caller's identity + role, classify the message into an intent,
/// dispatch to the right handler, and render the reply. Unmapped users get the FR-4.3 "point to HR" message.
/// </summary>
public sealed class EmployeeBot(
    CallerIdentityResolver identity,
    IConversationIntentService intentService,
    ConversationDispatcher dispatcher,
    TurnResponseRenderer renderer) : ActivityHandler
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

            // Conversation history (Redis, F3-S4) not wired yet - resolve each turn statelessly for now.
            IntentResult intent = await intentService.ResolveIntentAsync(
                turnContext.Activity.Text ?? string.Empty, [], cancellationToken);

            TurnResult result = await dispatcher.DispatchAsync(intent, caller, cancellationToken);
            await turnContext.SendActivityAsync(MessageFactory.Text(renderer.Render(result)), cancellationToken);
        }
        catch (Exception)
        {
            // BR-09: never surface a raw error to the user. Per-failure-mode handling (EDA/Graph/AOAI/Redis)
            // and logging/correlation are refined in F8-S1/S2.
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
                    MessageFactory.Text("Hi! Ask how you're tracking against your 100-hour goal, your history, peer standing, or to log a motivation."),
                    cancellationToken);
            }
        }
    }
}
