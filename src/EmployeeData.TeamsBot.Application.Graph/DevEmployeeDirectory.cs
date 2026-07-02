using EmployeeData.TeamsBot.Domain.Interfaces;

namespace EmployeeData.TeamsBot.Application.Graph;

/// <summary>
/// Dev-only <see cref="IEmployeeDirectory"/> that resolves every caller to a fixed employee number, so local and
/// demo runs work without Microsoft Graph identity (see <c>GraphOptions.DevEmployeeNumber</c>). Never used in prod.
/// </summary>
public sealed class DevEmployeeDirectory(int employeeNumber) : IEmployeeDirectory
{
    public Task<int?> GetEmployeeNumberAsync(string aadObjectId, CancellationToken ct) =>
        Task.FromResult<int?>(employeeNumber);
}
