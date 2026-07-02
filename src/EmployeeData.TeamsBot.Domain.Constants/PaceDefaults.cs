namespace EmployeeData.TeamsBot.Domain.Constants;

/// <summary>
/// Default pace tunables (AC-5, BR-02). Overridable via config (<c>Pace:*</c>, spec section 15 / TR-DC-02) so
/// v2 pro-rating and holiday handling need no code change.
/// </summary>
public static class PaceDefaults
{
    public const int MonthlyGoalHours = 100;

    public const int EarlyMonthThresholdPercent = 30;

    /// <summary>
    /// Max plausible in-office hours creditable to a single working day, used to tell a recoverable
    /// <c>Behind</c> from an unrecoverable <c>AtRisk</c>: if working every remaining day at this cap still can't
    /// reach the goal, the caller is at risk. Config-driven (<c>Pace:MaxHoursPerWorkingDay</c>).
    /// </summary>
    public const int MaxHoursPerWorkingDay = 10;
}
