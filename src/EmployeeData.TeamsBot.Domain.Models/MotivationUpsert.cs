namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>Upsert payload for <c>POST /api/motivation</c> (FR-2.1, BR-06). Key = employee + type + month.</summary>
public sealed record MotivationUpsert(
    int EmployeeNumber,
    int MotivationTypeId,
    string CalendarMonth,
    string Description);
