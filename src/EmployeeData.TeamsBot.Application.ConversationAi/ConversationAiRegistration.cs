using EmployeeData.TeamsBot.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeData.TeamsBot.Application.ConversationAi;

public static class ConversationAiRegistration
{
    public static IServiceCollection AddConversationAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConversationAiOptions>(configuration.GetSection(ConversationAiOptions.SectionName));
        services.AddSingleton<IConversationClient, AzureOpenAiConversationClient>();
        services.AddSingleton<IConversationIntentService, AzureOpenAiConversationIntentService>();
        return services;
    }
}
