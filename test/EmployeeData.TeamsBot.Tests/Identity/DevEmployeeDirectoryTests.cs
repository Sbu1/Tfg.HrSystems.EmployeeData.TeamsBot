using EmployeeData.TeamsBot.Application.Graph;

namespace EmployeeData.TeamsBot.Tests.Identity;

public sealed class DevEmployeeDirectoryTests
{
    [Fact]
    public async Task Resolves_every_caller_to_the_configured_number()
    {
        int? number = await new DevEmployeeDirectory(123456).GetEmployeeNumberAsync("any-aad-object-id", CancellationToken.None);

        Assert.Equal(123456, number);
    }
}
