namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>One month of history (FR-1.2): month/year, hours, and whether the 100h goal was met.</summary>
public sealed record MonthHours(
    int CalendarMonth,
    int CalendarYear,
    int Hours,
    bool GoalMet);
