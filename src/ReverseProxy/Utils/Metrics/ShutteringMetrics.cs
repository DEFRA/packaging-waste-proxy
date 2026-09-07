using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Model;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics;

[ExcludeFromCodeCoverage]
public sealed class ShutteringMetrics : IShutteringMetrics
{
    private readonly Counter<long> _shutteredResponses;

    public ShutteringMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Metrics.MeterName);
        _shutteredResponses = meter.CreateCounter<long>(
            Metrics.Names.ShutteredResponse,
            nameof(Unit.COUNT),
            "Count of responses served from a shuttered proxy route"
        );
    }

    public void ResponseReturned(string routeId)
    {
        _shutteredResponses.Add(1, new KeyValuePair<string, object?>(Metrics.Tags.RouteId, routeId));
    }
}
