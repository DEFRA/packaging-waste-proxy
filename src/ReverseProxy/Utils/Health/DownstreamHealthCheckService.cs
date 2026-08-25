namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Health;

public sealed class DownstreamHealthCheckService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "DownstreamHealth";

    public async Task<AggregateHealthReport> Check(CancellationToken cancellationToken)
    {
        var checks = GetDestinations().Select(destination => CheckDestination(destination, cancellationToken));
        var results = await Task.WhenAll(checks);
        var report = results.ToDictionary(result => result.Name, StringComparer.OrdinalIgnoreCase);
        var status = report.Values.All(result => result.Status == nameof(HealthStatus.Healthy))
            ? nameof(HealthStatus.Healthy)
            : nameof(HealthStatus.Unhealthy);

        return new AggregateHealthReport(status, report);
    }

    private IEnumerable<DownstreamDestination> GetDestinations() =>
        configuration
            .GetSection("ReverseProxy")
            .GetSection("Clusters")
            .GetChildren()
            .SelectMany(cluster =>
                cluster
                    .GetSection("Destinations")
                    .GetChildren()
                    .Select(destination => new DownstreamDestination(
                        $"{cluster.Key}:{destination.Key}",
                        destination["Address"] ?? ""
                    ))
            );

    private async Task<DownstreamHealthCheckResult> CheckDestination(
        DownstreamDestination destination,
        CancellationToken cancellationToken
    )
    {
        string? endpoint = null;

        try
        {
            endpoint = GetHealthEndpoint(destination.Address);
            using var response = await httpClientFactory
                .CreateClient(HttpClientName)
                .GetAsync(endpoint, cancellationToken);

            return new DownstreamHealthCheckResult(
                destination.Name,
                response.IsSuccessStatusCode ? nameof(HealthStatus.Healthy) : nameof(HealthStatus.Unhealthy),
                endpoint,
                (int)response.StatusCode
            );
        }
        catch (Exception)
        {
            return new DownstreamHealthCheckResult(
                destination.Name,
                nameof(HealthStatus.Unhealthy),
                endpoint ?? destination.Address,
                null
            );
        }
    }

    private static string GetHealthEndpoint(string address) => new Uri(new Uri(address), "health").ToString();

    private sealed record DownstreamDestination(string Name, string Address);
}

public sealed record AggregateHealthReport(
    string Status,
    IReadOnlyDictionary<string, DownstreamHealthCheckResult> Results
);

public sealed record DownstreamHealthCheckResult(string Name, string Status, string Endpoint, int? StatusCode);

internal enum HealthStatus
{
    Healthy,
    Unhealthy,
}
