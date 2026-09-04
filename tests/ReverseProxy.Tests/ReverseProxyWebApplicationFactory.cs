using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("ShutteringTests");
    }
}
