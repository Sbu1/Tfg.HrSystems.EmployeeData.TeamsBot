namespace EmployeeData.TeamsBot.Application.Models;

/// <summary>Request for the manager's team standing this month (FR-3.1). ManagerEmployeeNumber is the clamped caller.</summary>
public sealed record GetTeamThisMonthRequest(int ManagerEmployeeNumber);

/// <summary>Request for the manager's team history (FR-3.2), default 6 months.</summary>
public sealed record GetTeamHistoryRequest(int ManagerEmployeeNumber, int Months = 6);

/// <summary>Request for the manager's at-risk reports (FR-3.3).</summary>
public sealed record GetAtRiskRequest(int ManagerEmployeeNumber);
