namespace EmployeeData.TeamsBot.Application.Models;

/// <summary>Request for the caller's current-month standing (FR-1.1). EmployeeNumber is the clamped caller.</summary>
public sealed record GetMyHoursRequest(int EmployeeNumber);
