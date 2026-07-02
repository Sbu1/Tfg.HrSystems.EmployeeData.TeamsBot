using System.Text;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Presentation.Bot;

/// <summary>
/// Renders a <see cref="TurnResult"/> to a Teams reply. Text for now; rich Adaptive Cards + quick-action buttons
/// are the F4-S3 visual layer, swapped in here without touching the dispatcher.
/// </summary>
public sealed class TurnResponseRenderer
{
    public string Render(TurnResult result) => result.Message ?? RenderPayload(result.Payload);

    private static string RenderPayload(object? payload) => payload switch
    {
        EmployeeStanding standing => RenderStanding(standing),
        Leaderboard leaderboard => RenderLeaderboard(leaderboard),
        IReadOnlyList<MonthHours> history => RenderHistory(history),
        IReadOnlyList<TeamMemberStanding> team => RenderTeam(team),
        IReadOnlyList<TeamMemberHistory> teamHistory => RenderTeamHistory(teamHistory),
        IReadOnlyList<MotivationView> motivations => RenderMotivations(motivations),
        AddMotivationResult add => add.Updated ? "Updated your motivation." : "Logged your motivation.",
        _ => "Done."
    };

    private static string RenderStanding(EmployeeStanding s) =>
        $"You're at {s.MtdHours}h of {s.GoalHours}h this month ({s.Status}). {s.GapHours}h to go. As at {s.AsAt:yyyy-MM-dd}.";

    private static string RenderHistory(IReadOnlyList<MonthHours> history)
    {
        if (history.Count == 0)
        {
            return "No history yet.";
        }

        var sb = new StringBuilder("Your recent months:\n");
        foreach (MonthHours month in history)
        {
            sb.AppendLine($"- {month.CalendarYear}-{month.CalendarMonth:D2}: {month.Hours}h {(month.GoalMet ? "(goal met)" : "(missed)")}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string RenderLeaderboard(Leaderboard leaderboard)
    {
        var sb = new StringBuilder($"Peer standing (you're #{leaderboard.YourRank}):\n");
        foreach (LeaderboardRow row in leaderboard.Rows)
        {
            sb.AppendLine($"- #{row.Rank} {row.PlayerName}: {row.Hours}h{(row.IsYou ? " (you)" : string.Empty)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string RenderTeam(IReadOnlyList<TeamMemberStanding> team)
    {
        if (team.Count == 0)
        {
            return "No team data for this month.";
        }

        var sb = new StringBuilder("Your team this month:\n");
        foreach (TeamMemberStanding member in team)
        {
            string risk = member.ProjectedShortfall > 0 ? $", ~{member.ProjectedShortfall}h short" : string.Empty;
            sb.AppendLine($"- {member.EmployeeName}: {member.Hours}h ({member.Status}{risk})");
        }

        return sb.ToString().TrimEnd();
    }

    private static string RenderTeamHistory(IReadOnlyList<TeamMemberHistory> team)
    {
        if (team.Count == 0)
        {
            return "No team history.";
        }

        var sb = new StringBuilder("Team history:\n");
        foreach (TeamMemberHistory member in team)
        {
            string months = string.Join(", ", member.Months.Select(m => $"{m.CalendarYear}-{m.CalendarMonth:D2} {m.Hours}h{(m.GoalMet ? "*" : string.Empty)}"));
            sb.AppendLine($"- {member.EmployeeName}: {months}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string RenderMotivations(IReadOnlyList<MotivationView> motivations)
    {
        if (motivations.Count == 0)
        {
            return "None recorded.";
        }

        var sb = new StringBuilder("Motivations:\n");
        foreach (MotivationView motivation in motivations)
        {
            sb.AppendLine($"- {motivation.CalendarMonth} {motivation.MotivationTypeValue}: {motivation.Description}");
        }

        return sb.ToString().TrimEnd();
    }
}
