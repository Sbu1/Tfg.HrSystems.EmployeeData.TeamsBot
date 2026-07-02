namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>A motivation was refused before hitting the API - e.g. the month met the goal (BR-05) or is invalid.</summary>
public sealed class MotivationNotAllowedException(string reason) : Exception(reason);
