using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>Returns the caller's recent monthly history (FR-1.2): most-recent-first, goal-met flagged, capped at 6 (EC-02).</summary>
public sealed class GetMyHistoryHandler(
    IEmployeeDataClient client,
    int goalHours = PaceDefaults.MonthlyGoalHours)
    : IRequestHandler<GetMyHistoryRequest, IReadOnlyList<MonthHours>>
{
    private const int MaxMonths = 6;

    public async Task<IReadOnlyList<MonthHours>> HandleAsync(
        GetMyHistoryRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        EmployeeHours hours = await client.GetEmployeeAsync(request.EmployeeNumber, ct);

        return hours.Months
            .OrderByDescending(month => month.CalendarYear)
            .ThenByDescending(month => month.CalendarMonth)
            .Take(MaxMonths)
            .Select(month => new MonthHours(month.CalendarMonth, month.CalendarYear, month.Hours, month.Hours >= goalHours))
            .ToList();
    }
}
