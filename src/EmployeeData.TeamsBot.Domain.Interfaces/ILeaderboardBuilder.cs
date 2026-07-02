using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Domain.Interfaces;

/// <summary>
/// Merges the caller's own current-month figure into the masked peer set and ranks them (KA-05, FR-1.3, EC-04).
/// Takes the caller's hours directly rather than a full <c>EmployeeStanding</c> - it only needs the figure.
/// </summary>
public interface ILeaderboardBuilder
{
    Leaderboard Build(int selfHours, IReadOnlyList<LeaderboardRow> maskedPeers);
}
