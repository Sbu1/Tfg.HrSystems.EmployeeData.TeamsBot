using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>Turns a report's monthly hours into a current-month <see cref="TeamMemberStanding"/> with pace + shortfall.</summary>
internal static class TeamStandingMapper
{
    public static TeamMemberStanding ToStanding(TeamMemberMonths member, IPaceCalculator pace, DateOnly referenceDate, int goalHours)
    {
        MonthlyHours? latest = member.Months
            .OrderByDescending(month => month.CalendarYear)
            .ThenByDescending(month => month.CalendarMonth)
            .FirstOrDefault();

        if (latest is null)
        {
            return new TeamMemberStanding(member.EmployeeName, member.EmployeeNumber, 0, PaceStatus.TooEarly, 0); // no data (EC-07)
        }

        // Evaluate pace as at the latest month with data: the reference date (T-1) if that is the current month,
        // otherwise the end of that (completed) month - so prior-month data isn't judged against this month's calendar.
        DateOnly asAt = latest.CalendarYear == referenceDate.Year && latest.CalendarMonth == referenceDate.Month
            ? referenceDate
            : new DateOnly(latest.CalendarYear, latest.CalendarMonth, DateTime.DaysInMonth(latest.CalendarYear, latest.CalendarMonth));

        PaceResult result = pace.Evaluate(latest.Hours, asAt);
        int shortfall = result.Status is PaceStatus.Behind or PaceStatus.AtRisk
            ? Math.Max(0, goalHours - result.ProjectedMonthEndHours)
            : 0;

        return new TeamMemberStanding(member.EmployeeName, member.EmployeeNumber, latest.Hours, result.Status, shortfall);
    }
}
