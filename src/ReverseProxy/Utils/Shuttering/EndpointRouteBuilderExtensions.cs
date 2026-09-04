namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapShuttering(
        this IEndpointRouteBuilder endpoints,
        IEnumerable<ShutteredRoute> shutteredRoutes,
        ShutteringPageRenderer pageRenderer
    )
    {
        foreach (var route in shutteredRoutes)
        {
            endpoints
                .Map(route.MatchPath, context => pageRenderer.Write(context, route))
                .WithDisplayName($"Shuttering: {route.RouteId}")
                .WithOrder(-1);
        }

        return endpoints;
    }
}
