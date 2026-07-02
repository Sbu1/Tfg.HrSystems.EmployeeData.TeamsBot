using EmployeeData.TeamsBot.Application.Handlers;
using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>FR-2.1/BR-07 (types) and FR-2.2 (list) read handlers.</summary>
public sealed class MotivationReadHandlersTests
{
    [Fact]
    public async Task GetMotivationTypes_returns_the_api_types()
    {
        var client = new FakeEmployeeDataClient
        {
            MotivationTypes = [new MotivationType(1, "Annual Leave"), new MotivationType(6, "Sick Leave")]
        };

        IReadOnlyList<MotivationType> types =
            await new GetMotivationTypesHandler(client).HandleAsync(new GetMotivationTypesRequest(), null, CancellationToken.None);

        Assert.Equal(new[] { 1, 6 }, types.Select(t => t.Id));
    }

    [Fact]
    public async Task ListMotivations_returns_the_subjects_motivations()
    {
        var view = new MotivationView(4, "202606", 1, "Annual Leave", "on leave", DateTimeOffset.UnixEpoch);
        var client = new FakeEmployeeDataClient { Motivations = [view] };

        IReadOnlyList<MotivationView> result =
            await new ListMotivationsHandler(client).HandleAsync(new ListMotivationsRequest(1234567), null, CancellationToken.None);

        Assert.Equal(4, Assert.Single(result).Id);
    }
}
