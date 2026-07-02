using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>A manager's team history (FR-3.2): each direct report's monthly hours + goal-met, most recent first.</summary>
public sealed class GetTeamHistoryHandler(
    IEmployeeDataClient client,
    int goalHours = PaceDefaults.MonthlyGoalHours)
    : IRequestHandler<GetTeamHistoryRequest, IReadOnlyList<TeamMemberHistory>>
{
    public async Task<IReadOnlyList<TeamMemberHistory>> HandleAsync(
        GetTeamHistoryRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        IReadOnlyList<TeamMemberMonths> team = await client.GetManagerTeamAsync(request.ManagerEmployeeNumber, request.Months, ct);

        return team
            .Select(member => new TeamMemberHistory(
                member.EmployeeName,
                member.EmployeeNumber,
                member.Months
                    .OrderByDescending(month => month.CalendarYear)
                    .ThenByDescending(month => month.CalendarMonth)
                    .Select(month => new MonthHours(month.CalendarMonth, month.CalendarYear, month.Hours, month.Hours >= goalHours))
                    .ToList()))
            .ToList();
    }
}
