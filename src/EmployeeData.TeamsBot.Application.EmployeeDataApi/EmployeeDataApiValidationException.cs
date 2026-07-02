namespace EmployeeData.TeamsBot.Application.EmployeeDataApi;

/// <summary>A 400 from the Employee Data API (e.g. a motivation for a month that met the goal, BR-05).</summary>
public sealed class EmployeeDataApiValidationException(string detail) : Exception(detail);
