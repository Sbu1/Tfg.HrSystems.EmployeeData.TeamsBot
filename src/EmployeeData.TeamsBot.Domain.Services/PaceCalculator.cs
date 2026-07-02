using EmployeeData.TeamsBot.Domain.Constants;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Domain.Services;

/// <summary>
/// Working-day-linear pace (BR-01/02, spec section 22.3). Goal and early-month threshold are injected so they
/// stay config-driven (TR-DC-02); defaults come from <see cref="PaceDefaults"/>.
/// </summary>
public sealed class PaceCalculator : IPaceCalculator
{
    private readonly IWorkingDayCalendar _calendar;
    private readonly int _goalHours;
    private readonly int _earlyMonthThresholdPercent;
    private readonly int _maxHoursPerWorkingDay;

    public PaceCalculator(
        IWorkingDayCalendar calendar,
        int goalHours = PaceDefaults.MonthlyGoalHours,
        int earlyMonthThresholdPercent = PaceDefaults.EarlyMonthThresholdPercent,
        int maxHoursPerWorkingDay = PaceDefaults.MaxHoursPerWorkingDay)
    {
        _calendar = calendar;
        _goalHours = goalHours;
        _earlyMonthThresholdPercent = earlyMonthThresholdPercent;
        _maxHoursPerWorkingDay = maxHoursPerWorkingDay;
    }

    public PaceResult Evaluate(int mtdHours, DateOnly asAt)
    {
        int totalWorkingDays = _calendar.WorkingDaysInMonth(asAt.Year, asAt.Month);
        int elapsedWorkingDays = _calendar.WorkingDaysElapsed(asAt);

        double fractionElapsed = totalWorkingDays == 0 ? 0d : (double)elapsedWorkingDays / totalWorkingDays;

        // BR-02 early-month guard: too little of the month has elapsed to call a projection.
        if (elapsedWorkingDays == 0 || fractionElapsed * 100 < _earlyMonthThresholdPercent)
        {
            return new PaceResult(PaceStatus.TooEarly, ExpectedHours: 0, ProjectedMonthEndHours: 0);
        }

        int expected = (int)Math.Round(_goalHours * fractionElapsed, MidpointRounding.AwayFromZero);
        int projected = (int)Math.Round(mtdHours / fractionElapsed, MidpointRounding.AwayFromZero);

        if (mtdHours >= expected)
        {
            return new PaceResult(PaceStatus.OnTrack, expected, projected);
        }

        // Below the pace line. Distinguish a recoverable Behind from an unrecoverable AtRisk: at risk only when
        // working every remaining working day at the daily cap still can't reach the goal (product ruling).
        int remainingWorkingDays = totalWorkingDays - elapsedWorkingDays;
        int maxAchievable = mtdHours + remainingWorkingDays * _maxHoursPerWorkingDay;
        PaceStatus status = maxAchievable >= _goalHours ? PaceStatus.Behind : PaceStatus.AtRisk;

        return new PaceResult(status, expected, projected);
    }
}
