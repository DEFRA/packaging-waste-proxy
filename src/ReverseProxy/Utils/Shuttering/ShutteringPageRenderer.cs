using System.Text;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal sealed class ShutteringPageRenderer(IWebHostEnvironment environment)
{
    private const string ContentPlaceholder = "{{content}}";
    private readonly string _layout = File.ReadAllText(
        Path.Combine(environment.ContentRootPath, "Shuttering", "Layout.html")
    );

    public ShutteredPage Load(ShutteredRoute route)
    {
        var contentPath = ShutteringPageContentFiles.GetPath(environment.ContentRootPath, route.ClusterId);
        var content = File.ReadAllText(contentPath);

        return new ShutteredPage(route.RouteId, route.MatchPath, Encoding.UTF8.GetBytes(CreatePage(content)));
    }

    public static Task Write(HttpContext context, ShutteredPage page)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.ContentLength = page.Content.Length;

        if (HttpMethods.IsHead(context.Request.Method))
        {
            return Task.CompletedTask;
        }

        return context.Response.Body.WriteAsync(page.Content, context.RequestAborted).AsTask();
    }

    private string CreatePage(string content)
    {
        return _layout.Replace(ContentPlaceholder, content, StringComparison.Ordinal);
    }
}
