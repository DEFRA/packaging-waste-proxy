using Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests;

[CollectionDefinition(nameof(WebApplicationFactoryCollection), DisableParallelization = true)]
public sealed class WebApplicationFactoryCollection;

public sealed class ReverseProxyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
    }
}

public sealed class InvalidConfigurationReverseProxyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("InvalidConfiguration");
    }
}

public sealed class ShutteredReverseProxyWebApplicationFactory : WebApplicationFactory<Program>
{
    public ShutteringMetricsSpy ShutteringMetrics { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("ShutteringTests");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IShutteringMetrics>();
            services.AddSingleton<IShutteringMetrics>(ShutteringMetrics);
        });
    }
}

public sealed class ShutteringMetricsSpy : IShutteringMetrics
{
    public List<string> RouteIds { get; } = [];

    public void Reset() => RouteIds.Clear();

    public void ResponseReturned(string routeId)
    {
        RouteIds.Add(routeId);
    }
}
