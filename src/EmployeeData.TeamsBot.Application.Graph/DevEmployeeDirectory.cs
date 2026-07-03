using EmployeeData.TeamsBot.Domain.Interfaces;

namespace EmployeeData.TeamsBot.Application.Graph;

/// <summary>
/// Dev-only <see cref="IEmployeeDirectory"/>. If the caller key is a number - e.g. the Bot Framework Emulator's
/// "User ID" set to an employee number - that number is used, so you can switch employees from the Emulator
/// without restarting. Otherwise it falls back to the configured <c>Graph:DevEmployeeNumber</c>. Never used in prod.
/// </summary>
public sealed class DevEmployeeDirectory(int fallbackEmployeeNumber) : IEmployeeDirectory
{
    public Task<int?> GetEmployeeNumberAsync(string aadObjectId, CancellationToken ct) =>
        Task.FromResult<int?>(int.TryParse(aadObjectId, out int employeeNumber) ? employeeNumber : fallbackEmployeeNumber);
}
