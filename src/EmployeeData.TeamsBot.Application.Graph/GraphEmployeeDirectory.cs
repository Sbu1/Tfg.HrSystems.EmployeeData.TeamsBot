using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmployeeData.TeamsBot.Domain.Interfaces;

namespace EmployeeData.TeamsBot.Application.Graph;

/// <summary>
/// Resolves a Teams AAD object id to an employee number via Microsoft Graph (KA-01, FR-4.1, section 13.2):
/// <c>GET /v1.0/users/{aadObjectId}?$select=employeeId</c>. The <see cref="HttpClient"/> is expected to carry the
/// app (client-credentials) token - attached by the handler wired in <see cref="GraphDirectoryRegistration"/>.
/// A missing <c>employeeId</c> or a 404 means unmapped (null), which drives the FR-4.3 "point to HR" path.
/// </summary>
public sealed class GraphEmployeeDirectory(HttpClient http) : IEmployeeDirectory
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<int?> GetEmployeeNumberAsync(string aadObjectId, CancellationToken ct)
    {
        using HttpResponseMessage response = await http.GetAsync($"v1.0/users/{aadObjectId}?$select=employeeId", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        GraphUserDto? user = await response.Content.ReadFromJsonAsync<GraphUserDto>(JsonOptions, ct);
        return int.TryParse(user?.EmployeeId, out int employeeNumber) ? employeeNumber : null;
    }

    private sealed record GraphUserDto(string? EmployeeId);
}
