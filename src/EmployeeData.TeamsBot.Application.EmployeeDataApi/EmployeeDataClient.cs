using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Application.EmployeeDataApi;

/// <summary>
/// Typed <see cref="HttpClient"/> over the Employee Data API (KA-02, spec section 13.1). Maps raw JSON to domain
/// carriers and applies the status handling in section 22.4; transient faults are handled by the resilience
/// handler wired in <see cref="EmployeeDataApiRegistration"/>.
/// </summary>
public sealed class EmployeeDataClient(HttpClient http) : IEmployeeDataClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<EmployeeHours> GetEmployeeAsync(int employeeNumber, CancellationToken ct)
    {
        EmployeeResponseDto dto = await GetAsync<EmployeeResponseDto>(
            $"api/employee?employeeNumber={employeeNumber}", ct);

        var months = dto.EmployeeMonths
            .Select(m => new MonthlyHours(m.CalendarMonth, m.CalendarYear, m.TimeInHoursMonthToDate))
            .ToList();
        var peers = (dto.TeamCurrentMonth ?? [])
            .Select(p => new PeerHours(p.PlayerName, p.TimeInHoursMonthToDate))
            .ToList();
        string name = dto.EmployeeMonths.Count > 0 ? dto.EmployeeMonths[0].EmployeeName : string.Empty;

        return new EmployeeHours(name, months, peers);
    }

    public async Task<IReadOnlyList<TeamMemberMonths>> GetManagerTeamAsync(
        int managerEmployeeNumber, int months, CancellationToken ct)
    {
        List<TeamMemberDto> rows = await GetAsync<List<TeamMemberDto>>(
            $"api/managerteam?employeeNumber={managerEmployeeNumber}&months={months}", ct);

        return rows
            .GroupBy(r => r.EmployeeNumber)
            .Select(g => new TeamMemberMonths(
                g.First().EmployeeName,
                int.Parse(g.Key),
                g.Select(r => new MonthlyHours(r.CalendarMonth, r.CalendarYear, r.TimeInHoursMonthToDate)).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<MotivationView>> GetMotivationsAsync(
        int employeeNumber, int lastXMonths, CancellationToken ct)
    {
        List<MotivationDto> rows = await GetAsync<List<MotivationDto>>(
            $"api/motivation?employeeNumber={employeeNumber}&lastXMonths={lastXMonths}", ct);

        return rows
            .Select(m => new MotivationView(m.Id, m.CalendarMonth, m.MotivationTypeId, m.MotivationTypeValue, m.Description, m.CreatedDate))
            .ToList();
    }

    public async Task<int> AddMotivationAsync(MotivationUpsert request, CancellationToken ct)
    {
        var body = new MotivationUpsertDto(
            request.EmployeeNumber, request.MotivationTypeId, request.CalendarMonth, request.Description);

        using HttpResponseMessage response = await http.PostAsJsonAsync("api/motivation", body, JsonOptions, ct);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new EmployeeDataApiValidationException(await response.Content.ReadAsStringAsync(ct));
        }

        response.EnsureSuccessStatusCode();
        AddMotivationResponseDto? result = await response.Content.ReadFromJsonAsync<AddMotivationResponseDto>(JsonOptions, ct);
        return result?.Id ?? 0;
    }

    public async Task<bool> DeleteMotivationAsync(int motivationId, CancellationToken ct)
    {
        using HttpResponseMessage response = await http.DeleteAsync($"api/motivation/{motivationId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false; // already removed (section 22.4)
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<IReadOnlyList<MotivationType>> GetMotivationTypesAsync(CancellationToken ct)
    {
        List<MotivationTypeDto> rows = await GetAsync<List<MotivationTypeDto>>("api/motivationtype", ct);
        return rows.Select(t => new MotivationType(t.Id, t.Value)).ToList();
    }

    private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken ct)
    {
        using HttpResponseMessage response = await http.GetAsync(relativeUrl, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct)
            ?? throw new EmployeeDataApiValidationException($"Empty response from {relativeUrl}");
    }
}
