namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>One ranked, anonymised row in the peer leaderboard (FR-1.3). <see cref="IsYou"/> highlights the caller.</summary>
public sealed record LeaderboardRow(
    string PlayerName,
    int Hours,
    bool IsYou,
    int Rank);
