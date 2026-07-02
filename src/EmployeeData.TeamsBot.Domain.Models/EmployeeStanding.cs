namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>Caller's current-month standing (FR-1.1): MTD hours, gap to the goal, pace status, and data currency.</summary>
public sealed record EmployeeStanding(
    int MtdHours,
    int GoalHours,
    int GapHours,
    PaceStatus Status,
    DateOnly AsAt);
