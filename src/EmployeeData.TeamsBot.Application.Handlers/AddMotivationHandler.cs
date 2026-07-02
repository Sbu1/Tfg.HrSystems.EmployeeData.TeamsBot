using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>
/// Logs/upserts a motivation (FR-2.1). Enforces BR-05 (qualifying month) by reading the subject's hours before
/// posting - a completed month must be under goal, the current month must be behind/at-risk - and detects an
/// existing same-key entry for the BR-06 upsert flag. Refuses non-qualifying months rather than relying on a 400.
/// </summary>
public sealed class AddMotivationHandler(
    IEmployeeDataClient client,
    IPaceCalculator pace,
    TimeProvider time,
    int goalHours = PaceDefaults.MonthlyGoalHours)
    : IRequestHandler<AddMotivationRequest, AddMotivationResult>
{
    private const int LookbackMonths = 12;

    public async Task<AddMotivationResult> HandleAsync(
        AddMotivationRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        (int year, int month) = ParseCalendarMonth(request.CalendarMonth);

        int? subjectHours = await SubjectMonthHoursAsync(request, year, month, ct);
        EnsureQualifies(request.CalendarMonth, year, month, subjectHours ?? 0);

        IReadOnlyList<MotivationView> existing = await client.GetMotivationsAsync(request.TargetEmployeeNumber, LookbackMonths, ct);
        bool updated = existing.Any(m => m.CalendarMonth == request.CalendarMonth && m.MotivationTypeId == request.MotivationTypeId);

        int id = await client.AddMotivationAsync(
            new MotivationUpsert(request.TargetEmployeeNumber, request.MotivationTypeId, request.CalendarMonth, request.Description), ct);

        return new AddMotivationResult(id, updated);
    }

    private void EnsureQualifies(string calendarMonth, int year, int month, int subjectHours)
    {
        DateOnly today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        int requestedKey = year * 100 + month;
        int currentKey = today.Year * 100 + today.Month;

        bool qualifies = requestedKey.CompareTo(currentKey) switch
        {
            > 0 => false,                                                       // a future month never qualifies
            0 => pace.Evaluate(subjectHours, today.AddDays(-1)).Status is PaceStatus.Behind or PaceStatus.AtRisk,
            _ => subjectHours < goalHours                                       // a completed month must be under goal
        };

        if (!qualifies)
        {
            throw new MotivationNotAllowedException($"Month {calendarMonth} does not qualify for a motivation (BR-05).");
        }
    }

    private async Task<int?> SubjectMonthHoursAsync(AddMotivationRequest request, int year, int month, CancellationToken ct)
    {
        if (request.TargetEmployeeNumber == request.EmployeeNumber)
        {
            EmployeeHours self = await client.GetEmployeeAsync(request.EmployeeNumber, ct);
            return MatchMonth(self.Months, year, month);
        }

        IReadOnlyList<TeamMemberMonths> team = await client.GetManagerTeamAsync(request.EmployeeNumber, LookbackMonths, ct);
        TeamMemberMonths? member = team.FirstOrDefault(m => m.EmployeeNumber == request.TargetEmployeeNumber);
        return member is null ? null : MatchMonth(member.Months, year, month);
    }

    private static int? MatchMonth(IReadOnlyList<MonthlyHours> months, int year, int month) =>
        months.Where(m => m.CalendarYear == year && m.CalendarMonth == month).Select(m => (int?)m.Hours).FirstOrDefault();

    private static (int Year, int Month) ParseCalendarMonth(string yyyyMm)
    {
        if (yyyyMm.Length == 6
            && int.TryParse(yyyyMm.AsSpan(0, 4), out int year)
            && int.TryParse(yyyyMm.AsSpan(4, 2), out int month)
            && month is >= 1 and <= 12)
        {
            return (year, month);
        }

        throw new MotivationNotAllowedException($"Invalid month '{yyyyMm}' - expected YYYYMM.");
    }
}
