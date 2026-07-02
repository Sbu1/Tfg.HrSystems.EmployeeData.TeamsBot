using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>Returns the valid motivation types from the API (FR-2.1, BR-07 - never free-typed).</summary>
public sealed class GetMotivationTypesHandler(IEmployeeDataClient client)
    : IRequestHandler<GetMotivationTypesRequest, IReadOnlyList<MotivationType>>
{
    public Task<IReadOnlyList<MotivationType>> HandleAsync(
        GetMotivationTypesRequest request, Dictionary<string, string>? context, CancellationToken ct) =>
        client.GetMotivationTypesAsync(ct);
}
