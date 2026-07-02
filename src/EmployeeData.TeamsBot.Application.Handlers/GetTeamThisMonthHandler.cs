using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>A manager's team standing this month (FR-3.1): each direct report's current-month hours + pace status.</summary>
public sealed class GetTeamThisMonthHandler(
    IEmployeeDataClient client,
    IPaceCalculator pace,
    TimeProvider time,
    int goalHours = PaceDefaults.MonthlyGoalHours)
    : IRequestHandler<GetTeamThisMonthRequest, IReadOnlyList<TeamMemberStanding>>
{
    public async Task<IReadOnlyList<TeamMemberStanding>> HandleAsync(
        GetTeamThisMonthRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        DateOnly asAt = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddDays(-1);
        IReadOnlyList<TeamMemberMonths> team = await client.GetManagerTeamAsync(request.ManagerEmployeeNumber, months: 1, ct);

        return team.Select(member => TeamStandingMapper.ToStanding(member, pace, asAt, goalHours)).ToList();
    }
}
