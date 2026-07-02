using EmployeeData.TeamsBot.Application.Models;
using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Models;
using Tfg.Handler.Abstractions;

namespace EmployeeData.TeamsBot.Application.Handlers;

/// <summary>
/// Removes a motivation (FR-2.3). Verifies the id is in the subject's own set before deleting (BR-08 ownership)
/// so a caller can never delete another employee's motivation; a missing id is refused without an API call.
/// </summary>
public sealed class RemoveMotivationHandler(IEmployeeDataClient client)
    : IRequestHandler<RemoveMotivationRequest, RemoveMotivationResult>
{
    private const int OwnershipWindowMonths = 12;

    public async Task<RemoveMotivationResult> HandleAsync(
        RemoveMotivationRequest request, Dictionary<string, string>? context, CancellationToken ct)
    {
        IReadOnlyList<MotivationView> owned = await client.GetMotivationsAsync(request.EmployeeNumber, OwnershipWindowMonths, ct);
        if (!owned.Any(m => m.Id == request.MotivationId))
        {
            return new RemoveMotivationResult(false);
        }

        bool deleted = await client.DeleteMotivationAsync(request.MotivationId, ct);
        return new RemoveMotivationResult(deleted);
    }
}
