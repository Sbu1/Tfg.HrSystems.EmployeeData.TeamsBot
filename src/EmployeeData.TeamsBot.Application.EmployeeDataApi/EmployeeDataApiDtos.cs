namespace EmployeeData.TeamsBot.Application.EmployeeDataApi;

// Wire shapes for the Employee Data API (raw JSON, no envelope). Deserialised case-insensitively; only the
// fields the bot uses are modelled - timeOnSiteInSeconds and badges are intentionally ignored.

internal sealed record EmployeeResponseDto(
    List<EmployeeMonthDto> EmployeeMonths,
    List<PlayerDto>? TeamCurrentMonth);

internal sealed record EmployeeMonthDto(
    string EmployeeNumber,
    string EmployeeName,
    int CalendarMonth,
    int CalendarYear,
    int TimeInHoursMonthToDate);

internal sealed record PlayerDto(
    string PlayerName,
    int CalendarMonth,
    int CalendarYear,
    int TimeInHoursMonthToDate);

internal sealed record TeamMemberDto(
    string EmployeeNumber,
    string EmployeeName,
    int CalendarMonth,
    int CalendarYear,
    int TimeInHoursMonthToDate);

internal sealed record MotivationDto(
    int Id,
    string CalendarMonth,
    string Description,
    int MotivationTypeId,
    DateTimeOffset CreatedDate,
    string MotivationTypeValue);

internal sealed record MotivationTypeDto(int Id, string Value);

internal sealed record MotivationUpsertDto(
    int EmployeeNumber,
    int MotivationTypeId,
    string CalendarMonth,
    string Description);

internal sealed record AddMotivationResponseDto(int Id);
