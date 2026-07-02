namespace EmployeeData.TeamsBot.Tests.Handlers;

/// <summary>A <see cref="TimeProvider"/> pinned to a fixed instant so handler date maths are deterministic.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
