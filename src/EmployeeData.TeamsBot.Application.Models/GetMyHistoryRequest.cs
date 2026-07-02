namespace EmployeeData.TeamsBot.Application.Models;

/// <summary>Request for the caller's recent monthly history (FR-1.2). EmployeeNumber is the clamped caller.</summary>
public sealed record GetMyHistoryRequest(int EmployeeNumber);
