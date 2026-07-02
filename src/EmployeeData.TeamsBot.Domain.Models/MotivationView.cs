namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>A logged motivation as shown to the user (FR-2.2).</summary>
public sealed record MotivationView(
    int Id,
    string CalendarMonth,
    int MotivationTypeId,
    string MotivationTypeValue,
    string Description,
    DateTimeOffset CreatedDate);
