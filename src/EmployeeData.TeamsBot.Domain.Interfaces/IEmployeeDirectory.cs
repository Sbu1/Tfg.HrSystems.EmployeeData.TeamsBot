namespace EmployeeData.TeamsBot.Domain.Interfaces;

/// <summary>Maps a Teams user (AAD object id) to their employee number (KA-01, FR-4.1). Null when unmapped (FR-4.3).</summary>
public interface IEmployeeDirectory
{
    Task<int?> GetEmployeeNumberAsync(string aadObjectId, CancellationToken ct);
}
