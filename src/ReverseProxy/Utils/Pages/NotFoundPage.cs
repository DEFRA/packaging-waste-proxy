namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Pages;

internal sealed class NotFoundPage(ReadOnlyMemory<byte> content)
{
    public Task Write(HttpContext context)
    {
        if (AcceptsHtml(context.Request))
        {
            return PageRenderer.Write(context, content, StatusCodes.Status404NotFound);
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentLength = 0;

        return Task.CompletedTask;
    }

    private static bool AcceptsHtml(HttpRequest request)
    {
        return request
                .GetTypedHeaders()
                .Accept?.Any(accept =>
                    string.Equals(accept.MediaType.Value, "text/html", StringComparison.OrdinalIgnoreCase)
                    && accept.Quality.GetValueOrDefault(1) > 0
                )
            ?? false;
    }
}
