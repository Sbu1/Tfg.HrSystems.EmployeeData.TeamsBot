using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Domain.Services;

/// <summary>Merges the caller's own figure into the masked peers and ranks by hours (KA-05, FR-1.3, EC-03/EC-04).</summary>
public sealed class LeaderboardBuilder : ILeaderboardBuilder
{
    private const string YouLabel = "You";

    public Leaderboard Build(int selfHours, IReadOnlyList<LeaderboardRow> maskedPeers)
    {
        var entries = new List<(string Name, int Hours, bool IsYou)>(maskedPeers.Count + 1)
        {
            (YouLabel, selfHours, IsYou: true)
        };
        foreach (var peer in maskedPeers)
        {
            entries.Add((peer.PlayerName, peer.Hours, IsYou: false));
        }

        // Rank by hours desc; on ties, place peers ahead of "You" so the caller's rank is never flattered.
        var ordered = entries
            .OrderByDescending(entry => entry.Hours)
            .ThenBy(entry => entry.IsYou ? 1 : 0)
            .ToList();

        var rows = new List<LeaderboardRow>(ordered.Count);
        int yourRank = 0;
        for (int index = 0; index < ordered.Count; index++)
        {
            int rank = index + 1;
            (string name, int hours, bool isYou) = ordered[index];
            rows.Add(new LeaderboardRow(name, hours, isYou, rank));
            if (isYou)
            {
                yourRank = rank;
            }
        }

        return new Leaderboard(rows, yourRank);
    }
}
