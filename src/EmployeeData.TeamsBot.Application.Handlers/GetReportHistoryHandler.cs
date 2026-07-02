using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>One direct report's monthly history for a manager (FR-3.2 extension), most recent first + goal-met.</summary>
public sealed class GetReportHistoryHandler(
    IEmployeeDataClient client,
    int goalHours = PaceDefaults.MonthlyGoalHours)
    : IRequestHandler<GetReportHistoryRequest, TeamMemberHistory>
{
    public async Task<TeamMemberHistory> HandleAsync(
        GetReportHistoryRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        IReadOnlyList<TeamMemberMonths> team = await client.GetManagerTeamAsync(request.ManagerEmployeeNumber, request.Months, ct);
        TeamMemberMonths? member = team.FirstOrDefault(m => m.EmployeeNumber == request.TargetEmployeeNumber);

        IReadOnlyList<MonthHours> months = member is null
            ? []
            : member.Months
                .OrderByDescending(m => m.CalendarYear)
                .ThenByDescending(m => m.CalendarMonth)
                .Select(m => new MonthHours(m.CalendarMonth, m.CalendarYear, m.Hours, m.Hours >= goalHours))
                .ToList();

        return new TeamMemberHistory(member?.EmployeeName ?? string.Empty, request.TargetEmployeeNumber, months);
    }
}
