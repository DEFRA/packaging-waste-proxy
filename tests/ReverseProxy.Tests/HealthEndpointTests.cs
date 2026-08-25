using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests;

public class HealthEndpointTests(ReverseProxyWebApplicationFactory factory)
    : IClassFixture<ReverseProxyWebApplicationFactory>
{
    [Fact]
    public async Task GetHealth_ShouldReturnSuccess()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);

        body.Should().BeEquivalentTo(new HealthResponse("success"));
    }

    [Fact]
    public async Task GetHealth_WhenPortIsConfigured_ShouldReturnSuccess()
    {
        using var configuredPortFactory = new ReverseProxyWebApplicationFactory(
            "https://manage-recycling-obligations.example/",
            "0"
        );
        using var client = configuredPortFactory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void CreateClient_WhenDestinationAddressIsUnconfigured_ShouldThrow()
    {
        using var invalidConfigurationFactory = new ReverseProxyWebApplicationFactory("https://unconfigured.invalid/");

        Action act = () => invalidConfigurationFactory.CreateClient();

        act.Should().Throw<InvalidOperationException>().WithMessage("Application start-up failed.");
    }

    private sealed record HealthResponse(string Message);
}
