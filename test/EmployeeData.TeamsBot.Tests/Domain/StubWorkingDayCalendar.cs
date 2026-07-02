using EmployeeData.TeamsBot.Domain.Interfaces;

namespace EmployeeData.TeamsBot.Tests.Domain;

/// <summary>Fixed-count calendar so pace tests are deterministic and independent of the real weekday maths.</summary>
internal sealed class StubWorkingDayCalendar(int totalWorkingDays, int elapsedWorkingDays) : IWorkingDayCalendar
{
    public int WorkingDaysInMonth(int year, int month) => totalWorkingDays;

    public int WorkingDaysElapsed(DateOnly asAt) => elapsedWorkingDays;
}
