using EmployeeData.TeamsBot.Domain.Interfaces;
using EmployeeData.TeamsBot.Domain.Services;

namespace EmployeeData.TeamsBot.Presentation.Bot.Dependencies;

public static class DomainRegistration
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IWorkingDayCalendar, WorkingDayCalendar>();
        services.AddSingleton<IPaceCalculator, PaceCalculator>();
        services.AddSingleton<ILeaderboardBuilder, LeaderboardBuilder>();
        return services;
    }
}
