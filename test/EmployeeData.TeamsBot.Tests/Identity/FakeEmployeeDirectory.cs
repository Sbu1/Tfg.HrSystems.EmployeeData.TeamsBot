using EmployeeData.TeamsBot.Domain.Interfaces;

namespace EmployeeData.TeamsBot.Tests.Identity;

internal sealed class FakeEmployeeDirectory(int? employeeNumber) : IEmployeeDirectory
{
    public Task<int?> GetEmployeeNumberAsync(string aadObjectId, CancellationToken ct) => Task.FromResult(employeeNumber);
}
