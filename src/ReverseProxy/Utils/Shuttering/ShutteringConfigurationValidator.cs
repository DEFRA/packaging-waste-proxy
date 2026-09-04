namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal static class ShutteringConfigurationValidator
{
    public static void Validate(IConfigurationSection shutteringConfiguration, string contentRootPath)
    {
        var configuredPaths = shutteringConfiguration.Get<ShutteringOptions>()?.Paths ?? [];

        foreach (var configuredPath in configuredPaths)
            ValidatePath(configuredPath.Path);

        var missingContentFiles = configuredPaths
            .Select(x => x.Path!)
            .Where(path => !File.Exists(ShutteringPageContentFiles.GetPath(contentRootPath, path)))
            .Select(ShutteringPageContentFiles.GetDisplayPath)
            .ToArray();

        if (missingContentFiles.Length > 0)
        {
            throw new InvalidOperationException(
                $"The following shuttering content files must exist: {string.Join(", ", missingContentFiles)}."
            );
        }
    }

    private static void ValidatePath(string? path)
    {
        var configuredPath = path ?? string.Empty;
        var hasUnsafeSegment = configuredPath.Split('/').Any(segment => segment is "." or "..");
        var isHealthPath =
            configuredPath.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || configuredPath.StartsWith("/health/", StringComparison.OrdinalIgnoreCase);

        if (
            string.IsNullOrWhiteSpace(configuredPath)
            || !configuredPath.StartsWith('/')
            || configuredPath.Contains('?', StringComparison.Ordinal)
            || configuredPath.Contains('#', StringComparison.Ordinal)
            || configuredPath.Contains('\\')
            || (configuredPath.Length > 1 && configuredPath.EndsWith("/", StringComparison.Ordinal))
            || hasUnsafeSegment
            || isHealthPath
        )
        {
            throw new InvalidOperationException(
                $"Shuttering path '{configuredPath}' must be an absolute path without a trailing slash, cannot include health endpoints, and cannot contain . or .. segments."
            );
        }
    }
}
