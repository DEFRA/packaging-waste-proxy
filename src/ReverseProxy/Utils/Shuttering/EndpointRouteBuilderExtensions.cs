namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapShuttering(
        this IEndpointRouteBuilder endpoints,
        ShutteringOptions options,
        ShutteringPageRenderer pageRenderer
    )
    {
        foreach (var page in options.Paths.Where(x => x.Shuttered))
        {
            var path = page.Path!;
            endpoints
                .Map(GetRoutePattern(path), context => pageRenderer.Write(context, page))
                .WithDisplayName($"Shuttering: {path}")
                .WithOrder(-1);
        }

        return endpoints;
    }

    private static string GetRoutePattern(string path) => path == "/" ? "/{**catchAll}" : $"{path}/{{**catchAll}}";
}
