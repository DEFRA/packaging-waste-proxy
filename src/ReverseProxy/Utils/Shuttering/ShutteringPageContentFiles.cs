namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal static class ShutteringPageContentFiles
{
    private const string ContentDirectory = "Shuttering/Pages";

    public static string GetPath(string contentRootPath, string path) =>
        Path.Combine(contentRootPath, ContentDirectory, GetRelativePath(path));

    public static string GetRelativePath(string path) => path == "/" ? "index.html" : $"{path.TrimStart('/')}.html";

    public static string GetDisplayPath(string path) => $"{ContentDirectory}/{GetRelativePath(path)}";
}
