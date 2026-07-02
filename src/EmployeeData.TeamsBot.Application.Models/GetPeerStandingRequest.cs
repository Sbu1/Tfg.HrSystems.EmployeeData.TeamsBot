namespace EmployeeData.TeamsBot.Application.Models;

/// <summary>Request for the caller's anonymised peer standing (FR-1.3). EmployeeNumber is the clamped caller.</summary>
public sealed record GetPeerStandingRequest(int EmployeeNumber);
