using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Application.Validators;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>
/// Routes a classified <see cref="IntentResult"/> to the right use-case handler for a resolved caller (F4-S2):
/// resolves the model's names/phrases to concrete ids in code (never trusting the model), gates manager-only
/// intents (EC-05), and returns a <see cref="TurnResult"/> the Presentation layer renders. Write confirmation
/// (FR-4.4) and rich cards are layered on in Presentation (F4-S3/S4).
/// </summary>
public sealed class ConversationDispatcher(
    GetMyHoursHandler myHours,
    GetMyHistoryHandler myHistory,
    GetPeerStandingHandler peerStanding,
    GetMotivationTypesHandler motivationTypes,
    ListMotivationsHandler listMotivations,
    AddMotivationHandler addMotivation,
    RemoveMotivationHandler removeMotivation,
    GetTeamThisMonthHandler teamThisMonth,
    GetTeamHistoryHandler teamHistory,
    GetAtRiskHandler atRisk,
    IEmployeeDataClient employeeData,
    TimeProvider time)
{
    private const int LookbackMonths = 12;

    public async Task<TurnResult> DispatchAsync(IntentResult intent, CallerIdentity caller, CancellationToken ct)
    {
        switch (intent.Intent)
        {
            case IntentNames.GetMyHours:
                return TurnResult.Data(intent.Intent, await myHours.HandleAsync(new GetMyHoursRequest(caller.EmployeeNumber), null, ct));

            case IntentNames.GetMyHistory:
                return TurnResult.Data(intent.Intent, await myHistory.HandleAsync(new GetMyHistoryRequest(caller.EmployeeNumber), null, ct));

            case IntentNames.GetPeerStanding:
                return TurnResult.Data(intent.Intent, await peerStanding.HandleAsync(new GetPeerStandingRequest(caller.EmployeeNumber), null, ct));

            case IntentNames.ListMotivations:
                return await ListMotivationsAsync(intent, caller, ct);

            case IntentNames.AddMotivation:
                return await AddMotivationAsync(intent, caller, ct);

            case IntentNames.RemoveMotivation:
                return await RemoveMotivationAsync(intent, caller, ct);

            case IntentNames.AddMotivationConfirmed:
                return await CommitAddAsync(intent, caller, ct);

            case IntentNames.RemoveMotivationConfirmed:
                return await CommitRemoveAsync(intent, caller, ct);

            case IntentNames.GetTeamThisMonth:
                return await ManagerOnly(caller, async () =>
                    TurnResult.Data(intent.Intent, await teamThisMonth.HandleAsync(new GetTeamThisMonthRequest(caller.EmployeeNumber), null, ct)));

            case IntentNames.GetTeamHistory:
                return await ManagerOnly(caller, async () =>
                    TurnResult.Data(intent.Intent, await teamHistory.HandleAsync(new GetTeamHistoryRequest(caller.EmployeeNumber, MonthsArg(intent)), null, ct)));

            case IntentNames.GetAtRisk:
                return await ManagerOnly(caller, async () =>
                    TurnResult.Data(intent.Intent, await atRisk.HandleAsync(new GetAtRiskRequest(caller.EmployeeNumber), null, ct)));

            case IntentNames.Help:
                return TurnResult.Text(HelpText(caller.Role));

            default: // clarify + any unrecognised intent
                return TurnResult.Text(ClarifyText(intent));
        }
    }

    private async Task<TurnResult> ListMotivationsAsync(IntentResult intent, CallerIdentity caller, CancellationToken ct)
    {
        (int? target, bool nameUnresolved) = await ResolveTargetAsync(intent, caller, ct);
        if (nameUnresolved)
        {
            return TurnResult.Text("I couldn't find that person on your team.");
        }

        IReadOnlyList<MotivationView> motivations =
            await listMotivations.HandleAsync(new ListMotivationsRequest(caller.EmployeeNumber, target, LookbackMonths), null, ct);
        return TurnResult.Data(intent.Intent, motivations);
    }

    private async Task<TurnResult> AddMotivationAsync(IntentResult intent, CallerIdentity caller, CancellationToken ct)
    {
        DateOnly today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        if (!TryArg(intent, "monthPhrase", out string monthPhrase) || MonthResolver.Resolve(monthPhrase, today) is not { } month)
        {
            return TurnResult.Text("I couldn't work out which month you meant - try e.g. 'June' or '202506'.");
        }

        if (!TryArg(intent, "motivationType", out string typeName))
        {
            return TurnResult.Text("Which type of motivation is it?");
        }

        IReadOnlyList<MotivationType> types = await motivationTypes.HandleAsync(new GetMotivationTypesRequest(), null, ct);
        if (MotivationTypeResolver.Resolve(typeName, types) is not { } typeId)
        {
            return TurnResult.Text("I couldn't match that to a motivation type. Options: " + string.Join(", ", types.Select(t => t.Value)));
        }

        (int? target, bool nameUnresolved) = await ResolveTargetAsync(intent, caller, ct);
        if (nameUnresolved)
        {
            return TurnResult.Text("I couldn't find that person on your team.");
        }

        int subject = target ?? caller.EmployeeNumber;
        string description = intent.Arguments.GetValueOrDefault("description", string.Empty);
        string whose = subject == caller.EmployeeNumber ? "your" : "this report's";

        // FR-4.4: confirm before committing. The resolved params ride on the Confirm button and are re-clamped
        // on commit (CommitAddAsync) - the round-tripped values are never trusted.
        var confirmArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["motivationTypeId"] = typeId.ToString(),
            ["calendarMonth"] = month,
            ["targetEmployeeNumber"] = subject.ToString(),
            ["description"] = description
        };
        return TurnResult.Data("confirm",
            new Confirmation($"Log a '{typeName}' motivation for {whose} {month}?", IntentNames.AddMotivationConfirmed, confirmArgs));
    }

    private async Task<TurnResult> CommitAddAsync(IntentResult intent, CallerIdentity caller, CancellationToken ct)
    {
        if (!int.TryParse(intent.Arguments.GetValueOrDefault("motivationTypeId"), out int typeId)
            || !int.TryParse(intent.Arguments.GetValueOrDefault("targetEmployeeNumber"), out int target))
        {
            return TurnResult.Text("Something went wrong preparing that - please try again.");
        }

        string month = intent.Arguments.GetValueOrDefault("calendarMonth", string.Empty);
        string description = intent.Arguments.GetValueOrDefault("description", string.Empty);

        // Re-clamp: the target came back via a button payload, so verify it's the caller or a direct report (TR-03).
        if (target != caller.EmployeeNumber && !await IsDirectReportAsync(caller, target, ct))
        {
            return TurnResult.Text("Sorry, I can only log motivations for you or your direct reports.");
        }

        try
        {
            AddMotivationResult result = await addMotivation.HandleAsync(
                new AddMotivationRequest(caller.EmployeeNumber, target, typeId, month, description), null, ct);
            return TurnResult.Data(IntentNames.AddMotivation, result);
        }
        catch (MotivationNotAllowedException ex)
        {
            return TurnResult.Text($"I can't log that motivation - {ex.Message}");
        }
    }

    private async Task<TurnResult> CommitRemoveAsync(IntentResult intent, CallerIdentity caller, CancellationToken ct)
    {
        if (!int.TryParse(intent.Arguments.GetValueOrDefault("motivationId"), out int motivationId))
        {
            return TurnResult.Text("Something went wrong preparing that - please try again.");
        }

        // RemoveMotivationHandler re-checks ownership against the caller's own set (BR-08), so a tampered id is safe.
        RemoveMotivationResult result = await removeMotivation.HandleAsync(
            new RemoveMotivationRequest(caller.EmployeeNumber, motivationId), null, ct);
        return TurnResult.Text(result.Deleted ? "Removed that motivation." : "I couldn't remove that motivation.");
    }

    private async Task<bool> IsDirectReportAsync(CallerIdentity caller, int target, CancellationToken ct)
    {
        if (caller.Role != CallerRole.Manager)
        {
            return false;
        }

        IReadOnlyList<TeamMemberMonths> team = await employeeData.GetManagerTeamAsync(caller.EmployeeNumber, months: 1, ct);
        return team.Any(member => member.EmployeeNumber == target);
    }

    private async Task<TurnResult> RemoveMotivationAsync(IntentResult intent, CallerIdentity caller, CancellationToken ct)
    {
        if (!TryArg(intent, "selector", out string selector))
        {
            return TurnResult.Text("Which motivation should I remove?");
        }

        IReadOnlyList<MotivationView> owned = await employeeData.GetMotivationsAsync(caller.EmployeeNumber, LookbackMonths, ct);
        List<MotivationView> matches = owned.Where(m => MatchesSelector(m, selector)).ToList();

        return matches switch
        {
            // FR-4.4: confirm before deleting; the concrete id rides on the Confirm button (ownership re-checked on commit).
            [var only] => TurnResult.Data("confirm", new Confirmation(
                $"Remove your {only.CalendarMonth} {only.MotivationTypeValue} motivation?",
                IntentNames.RemoveMotivationConfirmed,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["motivationId"] = only.Id.ToString() })),
            [] => TurnResult.Text("I couldn't find a motivation matching that."),
            _ => TurnResult.Text("More than one motivation matches - please be more specific (month + type).")
        };
    }

    private async Task<(int? Target, bool NameUnresolved)> ResolveTargetAsync(IntentResult intent, CallerIdentity caller, CancellationToken ct)
    {
        if (!TryArg(intent, "targetEmployeeName", out string name))
        {
            return (null, false); // no target -> the caller themselves
        }

        if (caller.Role != CallerRole.Manager)
        {
            return (null, true); // an employee can't target anyone else
        }

        IReadOnlyList<TeamMemberMonths> team = await employeeData.GetManagerTeamAsync(caller.EmployeeNumber, months: 1, ct);
        List<TeamMemberMonths> matches = team.Where(m => m.EmployeeName.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        return matches.Count == 1 ? (matches[0].EmployeeNumber, false) : (null, true);
    }

    private static async Task<TurnResult> ManagerOnly(CallerIdentity caller, Func<Task<TurnResult>> action) =>
        caller.Role == CallerRole.Manager
            ? await action()
            : TurnResult.Text("That's a manager view - I couldn't find a team for you.");

    private static bool MatchesSelector(MotivationView motivation, string selector) =>
        motivation.MotivationTypeValue.Contains(selector, StringComparison.OrdinalIgnoreCase)
        || selector.Contains(motivation.MotivationTypeValue, StringComparison.OrdinalIgnoreCase)
        || selector.Contains(motivation.CalendarMonth, StringComparison.OrdinalIgnoreCase);

    private static int MonthsArg(IntentResult intent) =>
        TryArg(intent, "months", out string value) && int.TryParse(value, out int months) && months > 0 ? months : 6;

    private static bool TryArg(IntentResult intent, string key, out string value)
    {
        if (intent.Arguments.TryGetValue(key, out string? found) && !string.IsNullOrWhiteSpace(found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string HelpText(CallerRole role) =>
        role == CallerRole.Manager
            ? "I can show your hours, history, peer standing, your team this month, team history, who's at risk, and log/remove motivations."
            : "I can show your in-office hours this month, your history, how you compare to peers, and log or remove motivations.";

    private static string ClarifyText(IntentResult intent) =>
        intent.Arguments.TryGetValue("reason", out string? reason) && !string.IsNullOrWhiteSpace(reason)
            ? $"I'm not sure I follow - {reason}. Could you rephrase?"
            : "I'm not sure what you'd like - you can ask about your hours, history, peers, or motivations.";
}
