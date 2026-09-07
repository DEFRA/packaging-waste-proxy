using System.Diagnostics.CodeAnalysis;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics;

[ExcludeFromCodeCoverage]
public static class Metrics
{
    public const string MeterName = "Defra.PackagingWasteProxy";

    public static class Names
    {
        public const string ShutteredResponse = nameof(ShutteredResponse);
    }

    public static class Tags
    {
        public const string RouteId = nameof(RouteId);
    }
}
