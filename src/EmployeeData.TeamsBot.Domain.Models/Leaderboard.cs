namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>Ranked anonymised peer standing (FR-1.3) with the caller's own rank surfaced.</summary>
public sealed record Leaderboard(
    IReadOnlyList<LeaderboardRow> Rows,
    int YourRank);
