using EmployeeData.TeamsBot.Domain.Interfaces;

namespace EmployeeData.TeamsBot.Domain.Services;

/// <summary>Weekdays-only working-day calendar (BR-03: public holidays ignored in v1).</summary>
public sealed class WorkingDayCalendar : IWorkingDayCalendar
{
    public int WorkingDaysInMonth(int year, int month)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int count = 0;
        for (int day = 1; day <= daysInMonth; day++)
        {
            if (IsWorkingDay(new DateOnly(year, month, day)))
            {
                count++;
            }
        }

        return count;
    }

    public int WorkingDaysElapsed(DateOnly asAt)
    {
        int count = 0;
        for (int day = 1; day <= asAt.Day; day++)
        {
            if (IsWorkingDay(new DateOnly(asAt.Year, asAt.Month, day)))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsWorkingDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
