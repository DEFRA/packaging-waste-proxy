using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests;

[Collection(nameof(WebApplicationFactoryCollection))]
public sealed class DnsDestinationResolverTests(ReverseProxyWebApplicationFactory factory)
    : IClassFixture<ReverseProxyWebApplicationFactory>
{
    [Fact]
    public void DnsDestinationResolver_ShouldRefreshResolvedDestinationsEverySixtySeconds()
    {
        using var scope = factory.Services.CreateScope();

        var resolver = scope.ServiceProvider.GetRequiredService<IDestinationResolver>();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<DnsDestinationResolverOptions>>();

        resolver.GetType().FullName.Should().Be("Yarp.ReverseProxy.ServiceDiscovery.DnsDestinationResolver");
        options.CurrentValue.RefreshPeriod.Should().Be(TimeSpan.FromSeconds(60));
    }
}
