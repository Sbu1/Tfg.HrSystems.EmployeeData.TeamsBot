using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;

namespace EmployeeData.TeamsBot.Presentation.Bot.Dependencies;

public static class BotAdapterRegistration
{
    public static IServiceCollection AddBotAdapter(this IServiceCollection services)
    {
        // Reads MicrosoftApp* keys from configuration. Empty MicrosoftAppId => anonymous auth, so the Bot
        // Framework Emulator can connect locally without a Connector JWT (technical-spec CP-08 spike host).
        services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

        // CloudAdapter has two public constructors, so DI can't auto-select; pick the auth-based one explicitly.
        services.AddSingleton(sp => new CloudAdapter(
            sp.GetRequiredService<BotFrameworkAuthentication>(),
            sp.GetRequiredService<ILogger<CloudAdapter>>()));

        services.AddTransient<IBot, EmployeeBot>();
        return services;
    }
}
