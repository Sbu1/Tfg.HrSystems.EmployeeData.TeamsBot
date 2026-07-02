namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>
/// A direct report's standing for the manager views (FR-3.1, FR-3.3). <see cref="ProjectedShortfall"/> is the
/// hours the projected month-end total falls below the goal (0 when not at risk); at-risk lists sort by it desc.
/// </summary>
public sealed record TeamMemberStanding(
    string EmployeeName,
    int EmployeeNumber,
    int Hours,
    PaceStatus Status,
    int ProjectedShortfall);
