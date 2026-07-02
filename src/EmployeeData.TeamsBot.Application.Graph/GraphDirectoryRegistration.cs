using EmployeeData.TeamsBot.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EmployeeData.TeamsBot.Application.Graph;

public sealed class GraphOptions
{
    public const string SectionName = "Graph";

    public string BaseUrl { get; set; } = "https://graph.microsoft.com/";

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Dev-only escape hatch: when set, identity resolves to this employee number and Graph is never called - so
    /// local/demo runs work before Graph app credentials + TOQ-04 coverage are in place. Never set in prod.
    /// </summary>
    public int? DevEmployeeNumber { get; set; }
}

public static class GraphDirectoryRegistration
{
    public static IServiceCollection AddGraphDirectory(this IServiceCollection services, IConfiguration configuration)
    {
        GraphOptions options = configuration.GetSection(GraphOptions.SectionName).Get<GraphOptions>() ?? new GraphOptions();
        services.Configure<GraphOptions>(configuration.GetSection(GraphOptions.SectionName));

        if (options.DevEmployeeNumber is int devEmployeeNumber and > 0)
        {
            // Local/demo: skip Graph and resolve every caller to the configured dev employee number.
            services.AddSingleton<IEmployeeDirectory>(new DevEmployeeDirectory(devEmployeeNumber));
        }
        else
        {
            services.AddHttpClient<IEmployeeDirectory, GraphEmployeeDirectory>((provider, client) =>
            {
                GraphOptions graph = provider.GetRequiredService<IOptions<GraphOptions>>().Value;
                client.BaseAddress = new Uri(graph.BaseUrl.TrimEnd('/') + "/");
            })
            // TODO(F8-S3): attach the client-credentials bearer token via a DelegatingHandler once Graph creds
            // (TenantId/ClientId + Vault secret) are available and TOQ-04 identity coverage is confirmed.
            .AddStandardResilienceHandler();
        }

        services.AddScoped<CallerIdentityResolver>();
        return services;
    }
}
