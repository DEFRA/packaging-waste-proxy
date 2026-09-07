using System.Diagnostics.Metrics;
using AwesomeAssertions;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics;
using Microsoft.Extensions.DependencyInjection;
using ProxyMetrics = Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics.Metrics;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests.Utils.Metrics;

public class ShutteringMetricsTests
{
    [Fact]
    public void ResponseReturned_ShouldIncrementCounterForRoute()
    {
        var meterFactory = CreateMeterFactory();
        using var collector = new TestMetricCollector<long>(
            ProxyMetrics.MeterName,
            ProxyMetrics.Names.ShutteredResponse
        );
        var subject = new ShutteringMetrics(meterFactory);

        subject.ResponseReturned("ManageRecyclingObligations");

        var measurements = collector.GetMeasurementSnapshot();
        measurements.Should().ContainSingle().Which.Value.Should().Be(1);
        measurements[0]
            .Tags.Should()
            .ContainKey(ProxyMetrics.Tags.RouteId)
            .WhoseValue.Should()
            .Be("ManageRecyclingObligations");
    }

    private static IMeterFactory CreateMeterFactory()
    {
        var services = new ServiceCollection();
        services.AddMetrics();

        return services.BuildServiceProvider().GetRequiredService<IMeterFactory>();
    }
}
