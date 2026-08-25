using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace Defra.PackagingWasteProxy.ReverseProxy.IntegrationTests;

public class AggregateHealthTests : IntegrationTestBase
{
    private const string ApiKey = nameof(ApiKey);
    private const string ApiKeyHeader = "X-Health-Check-Token";

    [Fact]
    public async Task GetHealthAll_WithoutApiKey_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/health/all", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHealthAll_WithApiKey_ShouldReturnDownstreamHealth()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyHeader, ApiKey);

        var response = await client.GetAsync("/health/all", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.ToString().Should().Be("no-store");

        var report = await response.Content.ReadFromJsonAsync<AggregateHealthReport>(
            TestContext.Current.CancellationToken
        );

        report.Should().NotBeNull();
        var healthReport = report!;

        healthReport.Status.Should().Be("Healthy");
        healthReport
            .Results["ManageRecyclingObligations:Primary"]
            .Should()
            .BeEquivalentTo(
                new DownstreamHealthCheckResult(
                    "ManageRecyclingObligations:Primary",
                    "Healthy",
                    "http://downstream:8080/health",
                    (int)HttpStatusCode.OK
                )
            );
    }

    private sealed record AggregateHealthReport(string Status, Dictionary<string, DownstreamHealthCheckResult> Results);

    private sealed record DownstreamHealthCheckResult(string Name, string Status, string Endpoint, int? StatusCode);
}
