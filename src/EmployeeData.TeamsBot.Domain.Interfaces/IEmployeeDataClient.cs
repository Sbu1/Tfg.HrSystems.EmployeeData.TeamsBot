using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Domain.Interfaces;

/// <summary>All Employee Data API access (KA-02). Implementations map raw JSON to the domain carriers here.</summary>
public interface IEmployeeDataClient
{
    Task<EmployeeHours> GetEmployeeAsync(int employeeNumber, CancellationToken ct);

    /// <summary>Direct reports over the last <paramref name="months"/> months; an empty list means the caller is not a manager (EC-05).</summary>
    Task<IReadOnlyList<TeamMemberMonths>> GetManagerTeamAsync(int managerEmployeeNumber, int months, CancellationToken ct);

    Task<IReadOnlyList<MotivationView>> GetMotivationsAsync(int employeeNumber, int lastXMonths, CancellationToken ct);

    /// <summary>Upserts a motivation and returns its id. Throws on API validation failure (400).</summary>
    Task<int> AddMotivationAsync(MotivationUpsert request, CancellationToken ct);

    /// <summary>Deletes a motivation; returns false when the id no longer exists (404, treated as already-removed).</summary>
    Task<bool> DeleteMotivationAsync(int motivationId, CancellationToken ct);

    Task<IReadOnlyList<MotivationType>> GetMotivationTypesAsync(CancellationToken ct);
}
