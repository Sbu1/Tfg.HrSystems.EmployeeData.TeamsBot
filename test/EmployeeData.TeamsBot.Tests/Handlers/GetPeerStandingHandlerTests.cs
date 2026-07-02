using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-1.3 - peer standing: the caller's own figure merged into masked peers and ranked (EC-03/EC-04).</summary>
public sealed class GetPeerStandingHandlerTests
{
    private static GetPeerStandingHandler Build(EmployeeHours employee) =>
        new(new FakeEmployeeDataClient(employee), new LeaderboardBuilder());

    [Fact]
    public async Task Merges_caller_into_masked_peers_and_ranks()
    {
        var employee = new EmployeeHours(
            "Du Toit, Werner",
            [new MonthlyHours(6, 2026, 50)],
            [new PeerHours("player1", 43), new PeerHours("player2", 42)]);

        Leaderboard board =
            await Build(employee).HandleAsync(new GetPeerStandingRequest(123456), null, CancellationToken.None);

        Assert.Equal(3, board.Rows.Count);
        Assert.Equal(1, board.YourRank);
        Assert.True(board.Rows[0].IsYou);
    }

    [Fact]
    public async Task No_peers_shows_only_the_caller()
    {
        var employee = new EmployeeHours("Solo", [new MonthlyHours(6, 2026, 30)], []);

        Leaderboard board =
            await Build(employee).HandleAsync(new GetPeerStandingRequest(1), null, CancellationToken.None);

        LeaderboardRow only = Assert.Single(board.Rows);
        Assert.True(only.IsYou);
    }
}
