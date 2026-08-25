using Microsoft.Extensions.Options;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Health;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAggregateHealth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<HealthAllOptions>()
            .Bind(configuration.GetSection(HealthAllOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddHttpClient(DownstreamHealthCheckService.HttpClientName)
            .ConfigureHttpClient(
                (serviceProvider, httpClient) =>
                    httpClient.Timeout = TimeSpan.FromMilliseconds(
                        serviceProvider
                            .GetRequiredService<IOptions<HealthAllOptions>>()
                            .Value.DownstreamTimeoutMilliseconds
                    )
            );
        services.AddSingleton<DownstreamHealthCheckService>();

        return services;
    }
}
