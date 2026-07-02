namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>A direct report's raw monthly hours from <c>managerteam</c> (FR-3.x), before pace is applied.</summary>
public sealed record TeamMemberMonths(
    string EmployeeName,
    int EmployeeNumber,
    IReadOnlyList<MonthlyHours> Months);
