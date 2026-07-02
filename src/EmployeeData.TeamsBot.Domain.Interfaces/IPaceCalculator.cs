using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Domain.Interfaces;

/// <summary>Working-day-linear pace evaluation (KA-04, BR-01/02).</summary>
public interface IPaceCalculator
{
    PaceResult Evaluate(int mtdHours, DateOnly asAt);
}
