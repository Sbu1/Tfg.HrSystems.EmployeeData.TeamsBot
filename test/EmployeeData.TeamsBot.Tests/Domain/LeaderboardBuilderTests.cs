using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;

namespace EmployeeData.TeamsBot.Tests.Domain;

/// <summary>TS-02 - leaderboard merge: own figure merged + ranked, "you" highlighted (FR-1.3, EC-03/EC-04).</summary>
public sealed class LeaderboardBuilderTests
{
    private readonly LeaderboardBuilder _builder = new();

    private static LeaderboardRow Peer(string name, int hours) => new(name, hours, IsYou: false, Rank: 0);

    [Fact]
    public void Merges_self_and_ranks_by_hours_desc()
    {
        Leaderboard board = _builder.Build(selfHours: 50, [Peer("player1", 43), Peer("player2", 42)]);

        Assert.Equal(3, board.Rows.Count);
        Assert.Equal(1, board.YourRank);
        Assert.True(board.Rows[0].IsYou);
        Assert.Equal("You", board.Rows[0].PlayerName);
        Assert.Equal(new[] { 50, 43, 42 }, board.Rows.Select(r => r.Hours));
        Assert.Equal(new[] { 1, 2, 3 }, board.Rows.Select(r => r.Rank));
    }

    [Fact]
    public void Ranks_self_in_the_middle()
    {
        Leaderboard board = _builder.Build(selfHours: 45, [Peer("player1", 60), Peer("player2", 30)]);

        Assert.Equal(2, board.YourRank);
        Assert.True(board.Rows.Single(r => r.IsYou).Hours == 45);
    }

    [Fact]
    public void No_peers_shows_only_the_caller()
    {
        Leaderboard board = _builder.Build(selfHours: 30, []);

        LeaderboardRow only = Assert.Single(board.Rows);
        Assert.True(only.IsYou);
        Assert.Equal(1, board.YourRank);
    }

    [Fact]
    public void On_a_tie_peers_rank_ahead_of_you()
    {
        Leaderboard board = _builder.Build(selfHours: 42, [Peer("player1", 42)]);

        Assert.Equal(2, board.YourRank);
        Assert.False(board.Rows[0].IsYou);
    }
}
