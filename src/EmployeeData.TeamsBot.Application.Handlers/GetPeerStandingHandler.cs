using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>Builds the caller's anonymised peer standing (FR-1.3): own current-month figure merged into masked peers, ranked.</summary>
public sealed class GetPeerStandingHandler(
    IEmployeeDataClient client,
    ILeaderboardBuilder leaderboard)
    : IRequestHandler<GetPeerStandingRequest, Leaderboard>
{
    public async Task<Leaderboard> HandleAsync(
        GetPeerStandingRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        EmployeeHours hours = await client.GetEmployeeAsync(request.EmployeeNumber, ct);
        int selfHours = hours.Months.Count > 0 ? hours.Months[0].Hours : 0;

        var peers = hours.CurrentMonthPeers
            .Select(peer => new LeaderboardRow(peer.PlayerName, peer.Hours, IsYou: false, Rank: 0))
            .ToList();

        return leaderboard.Build(selfHours, peers);
    }
}
