namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>A resolved Teams caller (FR-4.1): their employee number and derived role.</summary>
public sealed record CallerIdentity(int EmployeeNumber, CallerRole Role);
