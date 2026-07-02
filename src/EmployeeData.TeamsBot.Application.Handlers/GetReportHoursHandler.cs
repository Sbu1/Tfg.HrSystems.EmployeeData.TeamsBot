using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>
/// One direct report's current-month standing for a manager (FR-3.1 extension). Reads from the manager-team set
/// (so the target is inherently scoped to the caller's reports); the dispatcher resolves the name and clamps first.
/// </summary>
public sealed class GetReportHoursHandler(
    IEmployeeDataClient client,
    IPaceCalculator pace,
    TimeProvider time,
    int goalHours = PaceDefaults.MonthlyGoalHours)
    : IRequestHandler<GetReportHoursRequest, TeamMemberStanding>
{
    public async Task<TeamMemberStanding> HandleAsync(
        GetReportHoursRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        DateOnly asAt = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddDays(-1);
        IReadOnlyList<TeamMemberMonths> team = await client.GetManagerTeamAsync(request.ManagerEmployeeNumber, months: 6, ct);
        TeamMemberMonths? member = team.FirstOrDefault(m => m.EmployeeNumber == request.TargetEmployeeNumber);

        return member is null
            ? new TeamMemberStanding(string.Empty, request.TargetEmployeeNumber, 0, PaceStatus.TooEarly, 0)
            : TeamStandingMapper.ToStanding(member, pace, asAt, goalHours);
    }
}
