namespace EmployeeData.TeamsBot.Domain.Models;

/// <summary>Raw hours for one month as returned by the Employee Data API (no goal/pace logic applied yet).</summary>
public sealed record MonthlyHours(int CalendarMonth, int CalendarYear, int Hours);

/// <summary>A masked current-month peer figure (FR-1.3): anonymised player name + hours.</summary>
public sealed record PeerHours(string PlayerName, int Hours);

/// <summary>
/// The caller's Employee Data API result (FR-1.1/1.2/1.3): their monthly figures (current + history) and the
/// masked current-month peers. Handlers turn these into <see cref="EmployeeStanding"/>, <see cref="MonthHours"/>,
/// and a <see cref="Leaderboard"/>.
/// </summary>
public sealed record EmployeeHours(
    string EmployeeName,
    IReadOnlyList<MonthlyHours> Months,
    IReadOnlyList<PeerHours> CurrentMonthPeers);
