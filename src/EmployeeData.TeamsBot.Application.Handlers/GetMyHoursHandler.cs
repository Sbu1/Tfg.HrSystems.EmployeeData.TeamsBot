using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>Builds the caller's current-month standing (FR-1.1): MTD hours, gap to goal, pace status, "as at" (T-1).</summary>
public sealed class GetMyHoursHandler(
    IEmployeeDataClient client,
    IPaceCalculator pace,
    TimeProvider time,
    int goalHours = PaceDefaults.MonthlyGoalHours)
    : IRequestHandler<GetMyHoursRequest, EmployeeStanding>
{
    public async Task<EmployeeStanding> HandleAsync(
        GetMyHoursRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        EmployeeHours hours = await client.GetEmployeeAsync(request.EmployeeNumber, ct);

        // The API lists the current month first; absent data reads as zero (EC-01/EC-07).
        int mtdHours = hours.Months.Count > 0 ? hours.Months[0].Hours : 0;

        // Data currency is T-1 (RA-DC-02): the standing is "as at" yesterday.
        DateOnly asAt = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddDays(-1);
        PaceResult paceResult = pace.Evaluate(mtdHours, asAt);
        int gap = Math.Max(0, goalHours - mtdHours);

        return new EmployeeStanding(mtdHours, goalHours, gap, paceResult.Status, asAt);
    }
}
