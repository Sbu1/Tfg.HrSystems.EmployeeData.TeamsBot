using EmployeeData.TeamsBot.Application.Validators;
using EmployeeData.TeamsBot.Domain.Models;

namespace EmployeeData.TeamsBot.Tests.Validators;

/// <summary>Code-side motivation-type resolution (BR-07): the name the model emitted -> a valid type id.</summary>
public sealed class MotivationTypeResolverTests
{
    private static readonly IReadOnlyList<MotivationType> Types =
    [
        new(1, "Annual Leave"),
        new(6, "Sick Leave"),
        new(9, "WFH with permission")
    ];

    [Theory]
    [InlineData("Annual Leave", 1)]
    [InlineData("annual leave", 1)]   // case-insensitive exact
    [InlineData("WFH", 9)]            // unique substring match
    public void Resolves_to_a_type_id(string name, int expected)
    {
        Assert.Equal(expected, MotivationTypeResolver.Resolve(name, Types));
    }

    [Theory]
    [InlineData("leave")]             // ambiguous - matches Annual + Sick
    [InlineData("holiday")]           // no match
    public void Ambiguous_or_unknown_returns_null(string name)
    {
        Assert.Null(MotivationTypeResolver.Resolve(name, Types));
    }
}
