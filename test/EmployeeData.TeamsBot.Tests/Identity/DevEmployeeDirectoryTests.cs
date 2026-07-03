using EmployeeData.TeamsBot.Application.Graph;

namespace EmployeeData.TeamsBot.Tests.Identity;

public sealed class DevEmployeeDirectoryTests
{
    [Fact]
    public async Task Non_numeric_caller_falls_back_to_the_configured_number()
    {
        int? number = await new DevEmployeeDirectory(123456).GetEmployeeNumberAsync("any-aad-object-id", CancellationToken.None);

        Assert.Equal(123456, number);
    }

    [Fact]
    public async Task Numeric_caller_id_is_used_as_the_employee_number()
    {
        int? number = await new DevEmployeeDirectory(123456).GetEmployeeNumberAsync("10412592", CancellationToken.None);

        Assert.Equal(10412592, number);
    }
}
