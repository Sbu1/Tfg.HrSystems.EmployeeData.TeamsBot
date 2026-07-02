using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-2.3 - RemoveMotivation: only deletes ids in the caller's own set (BR-08 ownership).</summary>
public sealed class RemoveMotivationHandlerTests
{
    private static readonly MotivationView Owned = new(4, "202605", 1, "Annual Leave", "x", DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task Owned_motivation_is_deleted()
    {
        var client = new FakeEmployeeDataClient { Motivations = [Owned], DeleteResult = true };

        RemoveMotivationResult result =
            await new RemoveMotivationHandler(client).HandleAsync(new RemoveMotivationRequest(1, 4), null, CancellationToken.None);

        Assert.True(result.Deleted);
        Assert.Equal(4, client.LastDeletedId);
    }

    [Fact]
    public async Task Unowned_motivation_is_refused_without_calling_delete()
    {
        var client = new FakeEmployeeDataClient { Motivations = [Owned] };

        RemoveMotivationResult result =
            await new RemoveMotivationHandler(client).HandleAsync(new RemoveMotivationRequest(1, 999), null, CancellationToken.None);

        Assert.False(result.Deleted);
        Assert.Null(client.LastDeletedId);
    }
}
