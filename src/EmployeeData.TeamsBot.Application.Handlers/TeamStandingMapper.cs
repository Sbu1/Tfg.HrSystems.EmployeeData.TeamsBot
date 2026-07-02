using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>Turns a report's monthly hours into a current-month <see cref="TeamMemberStanding"/> with pace + shortfall.</summary>
internal static class TeamStandingMapper
{
    public static TeamMemberStanding ToStanding(TeamMemberMonths member, IPaceCalculator pace, DateOnly asAt, int goalHours)
    {
        int currentHours = member.Months
            .OrderByDescending(month => month.CalendarYear)
            .ThenByDescending(month => month.CalendarMonth)
            .Select(month => (int?)month.Hours)
            .FirstOrDefault() ?? 0; // no data reads as 0 (EC-07)

        PaceResult pace_ = pace.Evaluate(currentHours, asAt);

        // Shortfall only applies once past the early-month guard and below goal projection.
        int shortfall = pace_.Status is PaceStatus.Behind or PaceStatus.AtRisk
            ? Math.Max(0, goalHours - pace_.ProjectedMonthEndHours)
            : 0;

        return new TeamMemberStanding(member.EmployeeName, member.EmployeeNumber, currentHours, pace_.Status, shortfall);
    }
}
