namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>A motivation type from the Employee Data API (FR-2.1, BR-07) - id + display value.</summary>
public sealed record MotivationType(
    int Id,
    string Value);
