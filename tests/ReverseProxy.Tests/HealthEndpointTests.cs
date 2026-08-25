using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests;

public class HealthEndpointTests(ReverseProxyWebApplicationFactory factory)
    : IClassFixture<ReverseProxyWebApplicationFactory>
{
    private const string ApiKey = nameof(ApiKey);
    private const string ApiKeyHeader = "X-Health-Check-Token";

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
    public async Task GetHealthAll_WhenApiKeyIsNotConfigured_ShouldReturnUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/all", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.ToString().Should().Be("no-store");
    }

    [Fact]
    public async Task GetHealthAll_WhenApiKeyIsValidAndDownstreamIsUnavailable_ShouldReturnServiceUnavailable()
    {
        using var aggregateHealthFactory = new ReverseProxyWebApplicationFactory(
            "http://127.0.0.1:1/",
            healthAllApiKey: ApiKey
        );
        using var client = aggregateHealthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyHeader, ApiKey);

        var response = await client.GetAsync("/health/all", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.ToString().Should().Be("no-store");
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
