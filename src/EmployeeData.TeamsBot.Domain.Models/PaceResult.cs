namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>
/// Outcome of a pace evaluation: the status plus the two figures behind it - the linear "expected so far"
/// and the projected month-end total. <see cref="ProjectedMonthEndHours"/> drives at-risk shortfall sorting.
/// </summary>
public readonly record struct PaceResult(PaceStatus Status, int ExpectedHours, int ProjectedMonthEndHours);
