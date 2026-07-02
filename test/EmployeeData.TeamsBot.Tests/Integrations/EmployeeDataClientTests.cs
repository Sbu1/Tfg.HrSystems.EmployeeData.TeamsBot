using System.Net;
using EmployeeData.TeamsBot.Application.EmployeeDataApi;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Tests.Integrations;

/// <summary>JSON -> domain mapping + status handling (spec section 13.1 / 22.4), exercised via a stub transport.</summary>
public sealed class EmployeeDataClientTests
{
    private static EmployeeDataClient ClientReturning(HttpStatusCode status, string json) =>
        new(new HttpClient(new StubHttpMessageHandler(status, json)) { BaseAddress = new Uri("https://api.test/") });

    [Fact]
    public async Task GetEmployeeAsync_maps_months_and_masked_peers()
    {
        const string json = """
        {
          "employeeMonths": [
            { "employeeNumber": "123456", "employeeName": "Du Toit, Werner", "calendarMonth": 6, "calendarYear": 2026, "timeOnSiteInSeconds": 86987, "timeInHoursMonthToDate": 84 },
            { "employeeNumber": "123456", "employeeName": "Du Toit, Werner", "calendarMonth": 5, "calendarYear": 2026, "timeOnSiteInSeconds": 418212, "timeInHoursMonthToDate": 116 }
          ],
          "teamCurrentMonth": [
            { "playerName": "player1", "calendarMonth": 6, "calendarYear": 2026, "timeInHoursMonthToDate": 43 }
          ]
        }
        """;

        EmployeeHours result = await ClientReturning(HttpStatusCode.OK, json).GetEmployeeAsync(123456, CancellationToken.None);

        Assert.Equal("Du Toit, Werner", result.EmployeeName);
        Assert.Equal(new[] { 84, 116 }, result.Months.Select(m => m.Hours));
        PeerHours peer = Assert.Single(result.CurrentMonthPeers);
        Assert.Equal(("player1", 43), (peer.PlayerName, peer.Hours));
    }

    [Fact]
    public async Task GetManagerTeamAsync_empty_array_means_not_a_manager()
    {
        IReadOnlyList<TeamMemberMonths> team =
            await ClientReturning(HttpStatusCode.OK, "[]").GetManagerTeamAsync(3333333, months: 2, CancellationToken.None);

        Assert.Empty(team);
    }

    [Fact]
    public async Task GetManagerTeamAsync_groups_months_per_report_and_ignores_badges()
    {
        const string json = """
        [
          { "employeeNumber": "1234567", "employeeName": "Du Toit, Werner", "calendarMonth": 6, "calendarYear": 2026, "timeInHoursMonthToDate": 124, "badges": [] },
          { "employeeNumber": "1234567", "employeeName": "Du Toit, Werner", "calendarMonth": 5, "calendarYear": 2026, "timeInHoursMonthToDate": 100, "badges": [] },
          { "employeeNumber": "1111111", "employeeName": "Duminy, JD", "calendarMonth": 6, "calendarYear": 2026, "timeInHoursMonthToDate": 142, "badges": [] }
        ]
        """;

        IReadOnlyList<TeamMemberMonths> team =
            await ClientReturning(HttpStatusCode.OK, json).GetManagerTeamAsync(3333333, months: 2, CancellationToken.None);

        Assert.Equal(2, team.Count);
        Assert.Equal(2, team.Single(t => t.EmployeeNumber == 1234567).Months.Count);
    }

    [Fact]
    public async Task DeleteMotivationAsync_404_returns_false()
    {
        Assert.False(await ClientReturning(HttpStatusCode.NotFound, "").DeleteMotivationAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task AddMotivationAsync_400_throws_validation()
    {
        EmployeeDataClient client = ClientReturning(HttpStatusCode.BadRequest, "that month met the goal");

        await Assert.ThrowsAsync<EmployeeDataApiValidationException>(
            () => client.AddMotivationAsync(new MotivationUpsert(1234567, 1, "202606", "leave"), CancellationToken.None));
    }
}
