using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>
/// A manager's at-risk reports (FR-3.3): those projected to miss the goal (BR-01 - pace status Behind or AtRisk),
/// sorted by largest projected shortfall. On-track and too-early reports are excluded.
/// </summary>
public sealed class GetAtRiskHandler(
    IEmployeeDataClient client,
    IPaceCalculator pace,
    TimeProvider time,
    int goalHours = PaceDefaults.MonthlyGoalHours)
    : IRequestHandler<GetAtRiskRequest, IReadOnlyList<TeamMemberStanding>>
{
    public async Task<IReadOnlyList<TeamMemberStanding>> HandleAsync(
        GetAtRiskRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        DateOnly asAt = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddDays(-1);
        // Fetch a window so a report with no current-month row yet still resolves to their latest standing.
        IReadOnlyList<TeamMemberMonths> team = await client.GetManagerTeamAsync(request.ManagerEmployeeNumber, months: 6, ct);

        return team
            .Select(member => TeamStandingMapper.ToStanding(member, pace, asAt, goalHours))
            .Where(standing => standing.Status is PaceStatus.Behind or PaceStatus.AtRisk)
            .OrderByDescending(standing => standing.ProjectedShortfall)
            .ToList();
    }
}
