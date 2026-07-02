using EmployeeData.TeamsBot.Application.Validators;

namespace EmployeeData.TeamsBot.Tests.Validators;

/// <summary>Code-side month resolution (spec section 11 / 13.3): phrase the model emitted -> YYYYMM, never the LLM's job.</summary>
public sealed class MonthResolverTests
{
    private static readonly DateOnly Today = new(2026, 6, 16); // June 2026

    [Theory]
    [InlineData("202605", "202605")]              // already YYYYMM -> passthrough
    [InlineData("this month", "202606")]
    [InlineData("current month", "202606")]
    [InlineData("last month", "202605")]
    [InlineData("previous month", "202605")]
    [InlineData("June", "202606")]                // current month by name
    [InlineData("june", "202606")]                // case-insensitive
    [InlineData("May", "202605")]                 // earlier this year
    [InlineData("Aug", "202508")]                 // future-this-year -> most recent past August (last year)
    public void Resolves_known_phrases(string phrase, string expected)
    {
        Assert.Equal(expected, MonthResolver.Resolve(phrase, Today));
    }

    [Theory]
    [InlineData("banana")]
    [InlineData("")]
    [InlineData("209913")]                        // invalid month component
    public void Unresolvable_phrases_return_null(string phrase)
    {
        Assert.Null(MonthResolver.Resolve(phrase, Today));
    }
}
