using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Application.Graph;

/// <summary>
/// Resolves a Teams caller (FR-4.1): AAD object id -> employee number (via the directory), then derives the role -
/// Manager when they have direct reports, Employee otherwise (TA-02/AC-3). Null means unmapped (FR-4.3).
/// </summary>
public sealed class CallerIdentityResolver(IEmployeeDirectory directory, IEmployeeDataClient employeeData)
{
    public async Task<CallerIdentity?> ResolveAsync(string aadObjectId, CancellationToken ct)
    {
        int? employeeNumber = await directory.GetEmployeeNumberAsync(aadObjectId, ct);
        if (employeeNumber is null)
        {
            return null;
        }

        IReadOnlyList<TeamMemberMonths> team = await employeeData.GetManagerTeamAsync(employeeNumber.Value, months: 1, ct);
        CallerRole role = team.Count > 0 ? CallerRole.Manager : CallerRole.Employee;

        return new CallerIdentity(employeeNumber.Value, role);
    }
}
