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
}

public static class GraphDirectoryRegistration
{
    public static IServiceCollection AddGraphDirectory(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GraphOptions>(configuration.GetSection(GraphOptions.SectionName));

        services.AddHttpClient<IEmployeeDirectory, GraphEmployeeDirectory>((provider, client) =>
        {
            GraphOptions options = provider.GetRequiredService<IOptions<GraphOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        })
        // TODO(F8-S3): attach the client-credentials bearer token via a DelegatingHandler once Graph creds
        // (TenantId/ClientId + Vault secret) are available and TOQ-04 identity coverage is confirmed.
        .AddStandardResilienceHandler();

        services.AddScoped<CallerIdentityResolver>();
        return services;
    }
}
