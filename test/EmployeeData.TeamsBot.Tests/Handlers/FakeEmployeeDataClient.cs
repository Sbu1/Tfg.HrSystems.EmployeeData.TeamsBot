using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>Configurable fake for handler tests - only the members a given test needs are set; captures writes.</summary>
internal sealed class FakeEmployeeDataClient(
    EmployeeHours? employee = null,
    IReadOnlyList<TeamMemberMonths>? team = null) : IEmployeeDataClient
{
    public IReadOnlyList<MotivationView> Motivations { get; init; } = [];
    public IReadOnlyList<MotivationType> MotivationTypes { get; init; } = [];
    public int AddResultId { get; init; } = 1;
    public bool DeleteResult { get; init; } = true;

    public MotivationUpsert? LastAdded { get; private set; }
    public int? LastDeletedId { get; private set; }

    public Task<EmployeeHours> GetEmployeeAsync(int employeeNumber, CancellationToken ct) =>
        Task.FromResult(employee ?? throw new InvalidOperationException("employee not configured"));

    public Task<IReadOnlyList<TeamMemberMonths>> GetManagerTeamAsync(int managerEmployeeNumber, int months, CancellationToken ct) =>
        Task.FromResult(team ?? throw new InvalidOperationException("team not configured"));

    public Task<IReadOnlyList<MotivationView>> GetMotivationsAsync(int employeeNumber, int lastXMonths, CancellationToken ct) =>
        Task.FromResult(Motivations);

    public Task<int> AddMotivationAsync(MotivationUpsert request, CancellationToken ct)
    {
        LastAdded = request;
        return Task.FromResult(AddResultId);
    }

    public Task<bool> DeleteMotivationAsync(int motivationId, CancellationToken ct)
    {
        LastDeletedId = motivationId;
        return Task.FromResult(DeleteResult);
    }

    public Task<IReadOnlyList<MotivationType>> GetMotivationTypesAsync(CancellationToken ct) =>
        Task.FromResult(MotivationTypes);
}
