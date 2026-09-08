namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

using Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics;

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapShuttering(
        this IEndpointRouteBuilder endpoints,
        IEnumerable<ShutteredPage> shutteredPages
    )
    {
        foreach (var page in shutteredPages)
        {
            endpoints
                .Map(
                    page.MatchPath,
                    context =>
                    {
                        context.RequestServices.GetRequiredService<IShutteringMetrics>().ResponseReturned(page.RouteId);

                        return ShutteringPageRenderer.Write(context, page);
                    }
                )
                .WithDisplayName($"Shuttering: {page.RouteId}")
                .WithOrder(-1);
        }

        return endpoints;
    }
}
