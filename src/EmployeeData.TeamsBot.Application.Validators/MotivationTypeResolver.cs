using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Application.Validators;

/// <summary>
/// Resolves the type name the model emits to a valid motivation type id against the live type list (BR-07 - ids
/// are never free-typed). Prefers a case-insensitive exact match, then a unique substring match; ambiguous or
/// unknown returns null so the caller clarifies.
/// </summary>
public static class MotivationTypeResolver
{
    public static int? Resolve(string typeName, IReadOnlyList<MotivationType> types)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        string name = typeName.Trim();

        List<MotivationType> exact = types
            .Where(type => string.Equals(type.Value, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exact.Count == 1)
        {
            return exact[0].Id;
        }

        List<MotivationType> substring = types
            .Where(type => type.Value.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return substring.Count == 1 ? substring[0].Id : null;
    }
}
