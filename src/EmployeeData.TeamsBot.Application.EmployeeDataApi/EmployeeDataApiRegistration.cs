using EmployeeData.TeamsBot.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EmployeeData.TeamsBot.Application.EmployeeDataApi;

public static class EmployeeDataApiRegistration
{
    public static IServiceCollection AddEmployeeDataApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmployeeDataApiOptions>(configuration.GetSection(EmployeeDataApiOptions.SectionName));

        services.AddHttpClient<IEmployeeDataClient, EmployeeDataClient>((provider, client) =>
        {
            EmployeeDataApiOptions options = provider.GetRequiredService<IOptions<EmployeeDataApiOptions>>().Value;
            // Trailing slash so the client's relative "api/..." paths resolve against the base correctly.
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
