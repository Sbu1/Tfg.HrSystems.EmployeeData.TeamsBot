using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>Lists motivations for the caller, or a report when a manager supplies a target (FR-2.2). Target is clamped upstream.</summary>
public sealed class ListMotivationsHandler(IEmployeeDataClient client)
    : IRequestHandler<ListMotivationsRequest, IReadOnlyList<MotivationView>>
{
    public Task<IReadOnlyList<MotivationView>> HandleAsync(
        ListMotivationsRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        int subject = request.TargetEmployeeNumber ?? request.EmployeeNumber;
        return client.GetMotivationsAsync(subject, request.LastXMonths, ct);
    }
}
