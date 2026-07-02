namespace EmployeeData.TeamsBot.Domain.Interfaces;

/// <summary>Counts working days (Mon-Fri; public holidays ignored in v1, BR-03) for pace maths.</summary>
public interface IWorkingDayCalendar
{
    int WorkingDaysInMonth(int year, int month);

    /// <summary>Working days from the 1st of <paramref name="asAt"/>'s month up to and including <paramref name="asAt"/>.</summary>
    int WorkingDaysElapsed(DateOnly asAt);
}
