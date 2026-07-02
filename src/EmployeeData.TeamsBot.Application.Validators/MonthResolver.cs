using System.Globalization;

namespace EmployeeData.TeamsBot.Application.Validators;

/// <summary>
/// Resolves the free-text month phrase the model emits into a concrete <c>YYYYMM</c> using the current date
/// (spec section 11 / 13.3 - the LLM never computes dates). Returns null when unresolvable, so the caller clarifies.
/// A bare month name resolves to its most recent occurrence that is not in the future.
/// </summary>
public static class MonthResolver
{
    private static readonly Dictionary<string, int> MonthNames = BuildMonthNames();

    public static string? Resolve(string monthPhrase, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(monthPhrase))
        {
            return null;
        }

        string phrase = monthPhrase.Trim();

        if (phrase.Length == 6
            && int.TryParse(phrase.AsSpan(0, 4), out _)
            && int.TryParse(phrase.AsSpan(4, 2), out int month)
            && month is >= 1 and <= 12)
        {
            return phrase;
        }

        switch (phrase.ToLowerInvariant())
        {
            case "this month":
            case "current month":
                return Key(today.Year, today.Month);
            case "last month":
            case "previous month":
                DateOnly previous = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
                return Key(previous.Year, previous.Month);
        }

        if (MonthNames.TryGetValue(phrase, out int namedMonth))
        {
            int year = namedMonth <= today.Month ? today.Year : today.Year - 1;
            return Key(year, namedMonth);
        }

        return null;
    }

    private static string Key(int year, int month) => $"{year:D4}{month:D2}";

    private static Dictionary<string, int> BuildMonthNames()
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        DateTimeFormatInfo format = CultureInfo.InvariantCulture.DateTimeFormat;
        for (int month = 1; month <= 12; month++)
        {
            map[format.GetMonthName(month)] = month;
            map[format.GetAbbreviatedMonthName(month)] = month;
        }

        return map;
    }
}
