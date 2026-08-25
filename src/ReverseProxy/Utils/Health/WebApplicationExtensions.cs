using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Health;

public static class WebApplicationExtensions
{
    public const string HealthAllApiKeyHeader = "X-Health-Check-Token";

    public static void MapAggregateHealth(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { message = "success" })).WithOrder(-1);
        app.MapGet(
                "/health/all",
                async (
                    HttpRequest request,
                    HttpResponse response,
                    IOptions<HealthAllOptions> options,
                    DownstreamHealthCheckService health,
                    CancellationToken cancellationToken
                ) =>
                {
                    response.Headers.CacheControl = "no-store";

                    if (!HasValidApiKey(request.Headers[HealthAllApiKeyHeader], options.Value.ApiKey))
                        return Results.Unauthorized();

                    var report = await health.Check(cancellationToken);

                    return report.Status == nameof(HealthStatus.Healthy)
                        ? Results.Ok(report)
                        : Results.Json(report, statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            )
            .WithOrder(-1);
    }

    internal static bool HasValidApiKey(string? actualApiKey, string expectedApiKey)
    {
        if (string.IsNullOrEmpty(actualApiKey) || string.IsNullOrEmpty(expectedApiKey))
            return false;

        var actual = Encoding.UTF8.GetBytes(actualApiKey);
        var expected = Encoding.UTF8.GetBytes(expectedApiKey);

        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
