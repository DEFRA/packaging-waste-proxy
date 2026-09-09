using System.Text;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Pages;

internal sealed class PageRenderer(IWebHostEnvironment environment)
{
    private const string ContentPlaceholder = "{{content}}";
    private const string TitlePlaceholder = "{{title}}";
    private readonly string _layout = File.ReadAllText(
        Path.Combine(environment.ContentRootPath, "Pages", "Layout.html")
    );

    public ReadOnlyMemory<byte> Load(string title, string contentPath)
    {
        var content = File.ReadAllText(contentPath);

        return Encoding.UTF8.GetBytes(CreatePage(title, content));
    }

    public static Task Write(HttpContext context, ReadOnlyMemory<byte> content, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.ContentLength = content.Length;

        if (HttpMethods.IsHead(context.Request.Method))
        {
            return Task.CompletedTask;
        }

        return context.Response.Body.WriteAsync(content, context.RequestAborted).AsTask();
    }

    private string CreatePage(string title, string content)
    {
        return _layout
            .Replace(TitlePlaceholder, title, StringComparison.Ordinal)
            .Replace(ContentPlaceholder, content, StringComparison.Ordinal);
    }
}
