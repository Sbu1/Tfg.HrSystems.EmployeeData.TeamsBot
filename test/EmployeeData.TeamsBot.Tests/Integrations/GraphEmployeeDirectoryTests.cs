using System.Net;
using EmployeeData.TeamsBot.Application.Graph;

namespace EmployeeData.TeamsBot.Tests.Integrations;

/// <summary>Graph identity mapping (FR-4.1, section 13.2): employeeId -> employee number; missing/404 -> null (unmapped).</summary>
public sealed class GraphEmployeeDirectoryTests
{
    private static GraphEmployeeDirectory DirectoryReturning(HttpStatusCode status, string json) =>
        new(new HttpClient(new StubHttpMessageHandler(status, json)) { BaseAddress = new Uri("https://graph.test/") });

    [Fact]
    public async Task Parses_employee_id()
    {
        int? number = await DirectoryReturning(HttpStatusCode.OK, """{ "employeeId": "123456" }""")
            .GetEmployeeNumberAsync("aad-1", CancellationToken.None);

        Assert.Equal(123456, number);
    }

    [Fact]
    public async Task Null_employee_id_is_unmapped()
    {
        int? number = await DirectoryReturning(HttpStatusCode.OK, """{ "employeeId": null }""")
            .GetEmployeeNumberAsync("aad-1", CancellationToken.None);

        Assert.Null(number);
    }

    [Fact]
    public async Task Not_found_is_unmapped()
    {
        int? number = await DirectoryReturning(HttpStatusCode.NotFound, "")
            .GetEmployeeNumberAsync("aad-1", CancellationToken.None);

        Assert.Null(number);
    }
}
