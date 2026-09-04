using Microsoft.Extensions.Options;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal static class WebApplicationExtensions
{
    public static IApplicationBuilder UseShuttering(this IApplicationBuilder app) =>
        app.UseMiddleware<ShutteringMiddleware>();
}

internal sealed class ShutteringMiddleware(
    RequestDelegate next,
    IOptions<ShutteringOptions> options,
    ShutteringPageRenderer pageRenderer
)
{
    public async Task Invoke(HttpContext context)
    {
        var page = options
            .Value.Paths.Where(x => x.Shuttered && x.Path is not null)
            .OrderByDescending(x => x.Path!.Length)
            .FirstOrDefault(x => context.Request.Path.StartsWithSegments(x.Path!));

        if (page is null)
        {
            await next(context);

            return;
        }

        await pageRenderer.Write(context, page);
    }
}
