using EmployeeData.TeamsBot.Application.Graph;
using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Tests.Handlers;

namespace EmployeeData.TeamsBot.Tests.Identity;

/// <summary>FR-4.1 - resolve the Teams user to an employee number + role (Manager when they have direct reports, TA-02).</summary>
public sealed class CallerIdentityResolverTests
{
    private static CallerIdentityResolver Build(int? employeeNumber, IReadOnlyList<TeamMemberMonths>? team) =>
        new(new FakeEmployeeDirectory(employeeNumber), new FakeEmployeeDataClient(team: team));

    [Fact]
    public async Task Unmapped_user_resolves_to_null()
    {
        CallerIdentity? identity = await Build(employeeNumber: null, team: []).ResolveAsync("aad-1", CancellationToken.None);

        Assert.Null(identity);
    }

    [Fact]
    public async Task Mapped_user_with_reports_is_a_manager()
    {
        CallerIdentity? identity = await Build(3333333, [new TeamMemberMonths("Report", 1, [])]).ResolveAsync("aad-1", CancellationToken.None);

        Assert.Equal(new CallerIdentity(3333333, CallerRole.Manager), identity);
    }

    [Fact]
    public async Task Mapped_user_without_reports_is_an_employee()
    {
        CallerIdentity? identity = await Build(123456, []).ResolveAsync("aad-1", CancellationToken.None);

        Assert.Equal(new CallerIdentity(123456, CallerRole.Employee), identity);
    }
}
