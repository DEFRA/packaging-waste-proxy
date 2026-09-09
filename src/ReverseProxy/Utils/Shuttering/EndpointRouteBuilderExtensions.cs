namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

using Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Pages;

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

                        return PageRenderer.Write(context, page.Content, StatusCodes.Status503ServiceUnavailable);
                    }
                )
                .WithDisplayName($"Shuttering: {page.RouteId}")
                .WithOrder(-1);
        }

        return endpoints;
    }
}
