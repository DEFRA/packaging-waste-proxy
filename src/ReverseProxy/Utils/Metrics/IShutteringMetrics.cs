namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics;

public interface IShutteringMetrics
{
    void ResponseReturned(string routeId);
}
