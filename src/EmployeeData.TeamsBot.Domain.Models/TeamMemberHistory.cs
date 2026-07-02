namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>
/// A direct report's monthly history for the manager team-history view (FR-3.2): per-month hours + goal-met.
/// Note: section 11 typed GetTeamHistory as <c>TeamMemberStanding</c>, which cannot carry per-month data; this
/// richer type represents FR-3.2 faithfully.
/// </summary>
public sealed record TeamMemberHistory(
    string EmployeeName,
    int EmployeeNumber,
    IReadOnlyList<MonthHours> Months);
