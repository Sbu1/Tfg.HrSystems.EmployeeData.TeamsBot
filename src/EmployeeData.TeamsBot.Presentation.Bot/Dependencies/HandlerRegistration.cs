using EmployeeData.TeamsBot.Application.Handlers;

namespace EmployeeData.TeamsBot.Presentation.Bot.Dependencies;

public static class HandlerRegistration
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetMyHoursHandler>();
        services.AddScoped<GetMyHistoryHandler>();
        services.AddScoped<GetPeerStandingHandler>();
        services.AddScoped<GetMotivationTypesHandler>();
        services.AddScoped<ListMotivationsHandler>();
        services.AddScoped<AddMotivationHandler>();
        services.AddScoped<RemoveMotivationHandler>();
        services.AddScoped<GetTeamThisMonthHandler>();
        services.AddScoped<GetTeamHistoryHandler>();
        services.AddScoped<GetAtRiskHandler>();
        services.AddScoped<ConversationDispatcher>();
        return services;
    }
}
