namespace EmployeeData.TeamsBot.Application.Models;

/// <summary>Request for the valid motivation types (FR-2.1, BR-07).</summary>
public sealed record GetMotivationTypesRequest();

/// <summary>Request to list motivations (FR-2.2). TargetEmployeeNumber is set only when a manager views a report's.</summary>
public sealed record ListMotivationsRequest(int EmployeeNumber, int? TargetEmployeeNumber = null, int LastXMonths = 6);

/// <summary>Request to log/upsert a motivation (FR-2.1). TargetEmployeeNumber is the subject (self, or a report for a manager).</summary>
public sealed record AddMotivationRequest(
    int EmployeeNumber,
    int TargetEmployeeNumber,
    int MotivationTypeId,
    string CalendarMonth,
    string Description);

/// <summary>Outcome of an add/upsert: the motivation id, and whether it replaced an existing same-key entry (BR-06).</summary>
public sealed record AddMotivationResult(int Id, bool Updated);

/// <summary>Request to remove a motivation (FR-2.3). The id must belong to the caller's set (BR-08).</summary>
public sealed record RemoveMotivationRequest(int EmployeeNumber, int MotivationId);

/// <summary>Outcome of a remove: whether a motivation was deleted.</summary>
public sealed record RemoveMotivationResult(bool Deleted);
