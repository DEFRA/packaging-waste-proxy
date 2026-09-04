namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

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
                .Map(page.MatchPath, context => ShutteringPageRenderer.Write(context, page))
                .WithDisplayName($"Shuttering: {page.RouteId}")
                .WithOrder(-1);
        }

        return endpoints;
    }
}
