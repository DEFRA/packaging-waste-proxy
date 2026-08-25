using System.Collections.Concurrent;
using System.Net;
using AwesomeAssertions;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Health;
using Microsoft.Extensions.Configuration;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests.Utils.Health;

public class DownstreamHealthCheckServiceTests
{
    [Fact]
    public async Task Check_WhenAllDownstreamsAreHealthy_ShouldReturnHealthyReport()
    {
        var requests = new ConcurrentBag<Uri>();
        var health = CreateHealthCheckService(
            request =>
            {
                requests.Add(request.RequestUri!);

                return new HttpResponseMessage(HttpStatusCode.OK);
            },
            ("ManageRecyclingObligations", "Primary", "https://manage-recycling-obligations.example/"),
            ("ExampleService", "Primary", "https://example-service.example/base/")
        );

        var report = await health.Check(TestContext.Current.CancellationToken);

        report.Status.Should().Be("Healthy");
        report.Results.Should().OnlyContain(result => result.Value.Status == "Healthy");
        requests
            .Select(request => request.ToString())
            .Should()
            .BeEquivalentTo(
                "https://manage-recycling-obligations.example/health",
                "https://example-service.example/base/health"
            );
    }

    [Fact]
    public async Task Check_WhenDownstreamReturnsFailure_ShouldReturnUnhealthyReport()
    {
        var health = CreateHealthCheckService(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            ("ManageRecyclingObligations", "Primary", "https://manage-recycling-obligations.example/")
        );

        var report = await health.Check(TestContext.Current.CancellationToken);

        report.Status.Should().Be("Unhealthy");
        report
            .Results["ManageRecyclingObligations:Primary"]
            .Should()
            .BeEquivalentTo(
                new DownstreamHealthCheckResult(
                    "ManageRecyclingObligations:Primary",
                    "Unhealthy",
                    "https://manage-recycling-obligations.example/health",
                    (int)HttpStatusCode.ServiceUnavailable
                )
            );
    }

    [Fact]
    public async Task Check_WhenDownstreamCannotBeReached_ShouldReturnUnhealthyReport()
    {
        var health = CreateHealthCheckService(
            _ => throw new HttpRequestException(),
            ("ManageRecyclingObligations", "Primary", "https://manage-recycling-obligations.example/")
        );

        var report = await health.Check(TestContext.Current.CancellationToken);

        report.Status.Should().Be("Unhealthy");
        report.Results["ManageRecyclingObligations:Primary"].Status.Should().Be("Unhealthy");
        report.Results["ManageRecyclingObligations:Primary"].StatusCode.Should().BeNull();
    }

    [Fact]
    public async Task Check_WhenDownstreamAddressIsInvalid_ShouldReturnUnhealthyReport()
    {
        var health = CreateHealthCheckService(
            _ => throw new InvalidOperationException("The health request should not be sent."),
            ("ManageRecyclingObligations", "Primary", "not-a-uri")
        );

        var report = await health.Check(TestContext.Current.CancellationToken);

        report.Status.Should().Be("Unhealthy");
        report.Results["ManageRecyclingObligations:Primary"].Endpoint.Should().Be("not-a-uri");
        report.Results["ManageRecyclingObligations:Primary"].StatusCode.Should().BeNull();
    }

    private static DownstreamHealthCheckService CreateHealthCheckService(
        Func<HttpRequestMessage, HttpResponseMessage> response,
        params (string Cluster, string Destination, string Address)[] destinations
    )
    {
        var values = destinations.ToDictionary(
            destination =>
                $"ReverseProxy:Clusters:{destination.Cluster}:Destinations:{destination.Destination}:Address",
            destination => (string?)destination.Address
        );
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        return new DownstreamHealthCheckService(
            configuration,
            new TestHttpClientFactory(new TestHttpMessageHandler(response))
        );
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false);
    }

    private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(response(request));
    }
}
