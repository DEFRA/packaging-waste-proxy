using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace Defra.PackagingWasteProxy.ReverseProxy.IntegrationTests;

public class RoutingTests : IntegrationTestBase
{
    private const string TraceId = "4d2b9f4e-24de-467a-951f-342579445b2a";

    [Fact]
    public async Task ManageRecyclingObligations_ShouldRemovePublicPrefixAndForwardIt()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/manage-recycling-obligations/returns?year=2026",
            new { reference = "example" },
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var downstreamRequest = await response.Content.ReadFromJsonAsync<DownstreamRequest>(
            TestContext.Current.CancellationToken
        );

        downstreamRequest.Should().NotBeNull();
        downstreamRequest.Method.Should().Be(HttpMethod.Post.Method);
        downstreamRequest.Path.Should().Be("/returns");
        downstreamRequest.Query.Should().Be("?year=2026");
    }

    [Fact]
    public async Task ManageRecyclingObligations_WhenTraceHeaderReceived_ShouldForwardTraceHeader()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(TraceHeaderName, TraceId);

        var response = await client.PostAsJsonAsync(
            "/manage-recycling-obligations/trace-returns?year=2026",
            new { reference = "example" },
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var downstreamRequest = await response.Content.ReadFromJsonAsync<DownstreamRequest>(
            TestContext.Current.CancellationToken
        );

        downstreamRequest.Should().NotBeNull();
        downstreamRequest.CorrelationId.Should().Be(TraceId);
    }

    [Fact]
    public async Task ShutteredProxy_ShouldReturnItsMountedHoldingPageInsteadOfProxying()
    {
        using var client = CreateClient();

        var response = await client.GetAsync(
            "/shuttered-proxy/pages/a-nested-page.html",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.Location.Should().BeNull();
        response.Headers.CacheControl!.ToString().Should().Be("no-store");

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        content.Should().Contain("This path is shuttered for integration testing");
        content.Should().Contain("not part of the proxy image");
    }

    [Fact]
    public async Task UnpermittedPath_ShouldReturnPageNotFound()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/not-permitted");
        request.Headers.Accept.ParseAdd("text/html");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        response.Headers.CacheControl!.ToString().Should().Be("no-store");

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        content.Should().Contain("<title>Page not found</title>");
        content.Should().Contain("<h1 class=\"govuk-heading-l\">Page not found</h1>");
        content.Should().Contain("<div class=\"govuk-body\">");
    }

    private sealed record DownstreamRequest(string Method, string? Path, string? Query, string? CorrelationId);
}
