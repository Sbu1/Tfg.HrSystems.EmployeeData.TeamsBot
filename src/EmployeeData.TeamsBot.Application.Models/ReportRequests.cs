namespace EmployeeData.TeamsBot.Application.Models;

/// <summary>Manager request for one direct report's current-month standing (FR-3.1 extension). Target is clamped to the caller's team.</summary>
public sealed record GetReportHoursRequest(int ManagerEmployeeNumber, int TargetEmployeeNumber);

/// <summary>Manager request for one direct report's monthly history (FR-3.2 extension), default 6 months.</summary>
public sealed record GetReportHistoryRequest(int ManagerEmployeeNumber, int TargetEmployeeNumber, int Months = 6);
