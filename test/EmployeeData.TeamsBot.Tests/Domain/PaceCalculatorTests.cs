using EmployeeData.TeamsBot.Domain.Models;
using EmployeeData.TeamsBot.Domain.Services;

namespace EmployeeData.TeamsBot.Tests.Domain;

/// <summary>TS-01 - pace calc over seeded MTD + elapsed working days (BR-01/02, spec section 22.3).</summary>
public sealed class PaceCalculatorTests
{
    private static PaceCalculator Build(int totalWorkingDays, int elapsedWorkingDays) =>
        new(new StubWorkingDayCalendar(totalWorkingDays, elapsedWorkingDays),
            goalHours: 100,
            earlyMonthThresholdPercent: 30,
            maxHoursPerWorkingDay: 10);

    private static readonly DateOnly AnyDate = new(2026, 6, 15);

    [Fact]
    public void Below_early_month_threshold_is_TooEarly()
    {
        // 2 of 20 working days = 10% elapsed, under the 30% guard (BR-02).
        PaceResult result = Build(totalWorkingDays: 20, elapsedWorkingDays: 2).Evaluate(mtdHours: 5, AnyDate);

        Assert.Equal(PaceStatus.TooEarly, result.Status);
    }

    [Fact]
    public void Zero_elapsed_working_days_is_TooEarly()
    {
        PaceResult result = Build(totalWorkingDays: 20, elapsedWorkingDays: 0).Evaluate(mtdHours: 0, AnyDate);

        Assert.Equal(PaceStatus.TooEarly, result.Status);
    }

    [Fact]
    public void At_or_above_expected_is_OnTrack()
    {
        // Halfway (10/20): expected 50. MTD 60 >= 50 -> OnTrack; projected 120.
        PaceResult result = Build(totalWorkingDays: 20, elapsedWorkingDays: 10).Evaluate(mtdHours: 60, AnyDate);

        Assert.Equal(PaceStatus.OnTrack, result.Status);
        Assert.Equal(50, result.ExpectedHours);
        Assert.Equal(120, result.ProjectedMonthEndHours);
    }

    [Fact]
    public void Exactly_on_the_pace_line_is_OnTrack()
    {
        // MTD 50 == expected 50 -> OnTrack (boundary is inclusive).
        PaceResult result = Build(totalWorkingDays: 20, elapsedWorkingDays: 10).Evaluate(mtdHours: 50, AnyDate);

        Assert.Equal(PaceStatus.OnTrack, result.Status);
    }

    [Fact]
    public void Below_pace_but_recoverable_is_Behind()
    {
        // Halfway, MTD 40: below the expected 50, but 10 working days x 10h = 140 achievable >= 100 -> Behind.
        PaceResult result = Build(totalWorkingDays: 20, elapsedWorkingDays: 10).Evaluate(mtdHours: 40, AnyDate);

        Assert.Equal(PaceStatus.Behind, result.Status);
        Assert.Equal(50, result.ExpectedHours);
    }

    [Fact]
    public void Below_pace_and_unrecoverable_is_AtRisk()
    {
        // Late (18/20), MTD 30: only 2 working days left x 10h = 20, so max 50 < 100 -> cannot recover, AtRisk.
        PaceResult result = Build(totalWorkingDays: 20, elapsedWorkingDays: 18).Evaluate(mtdHours: 30, AnyDate);

        Assert.Equal(PaceStatus.AtRisk, result.Status);
    }
}
