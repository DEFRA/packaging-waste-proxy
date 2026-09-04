namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal static class ShutteringConfigurationValidator
{
    private const string ShutteredMetadataKey = "Shuttered";

    public static IReadOnlyList<ShutteredRoute> Validate(
        IConfigurationSection reverseProxyConfiguration,
        string contentRootPath
    )
    {
        var clusters = reverseProxyConfiguration
            .GetSection("Clusters")
            .GetChildren()
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var shutteredRoutes = reverseProxyConfiguration
            .GetSection("Routes")
            .GetChildren()
            .Select(GetShutteredRoute)
            .OfType<ShutteredRoute>()
            .ToArray();

        foreach (var shutteredRoute in shutteredRoutes)
        {
            ValidateCluster(shutteredRoute, clusters);
            ValidateMatchPath(shutteredRoute);
        }

        ValidateUniqueMatchPaths(shutteredRoutes);

        var missingContentFiles = shutteredRoutes
            .Where(route => !File.Exists(ShutteringPageContentFiles.GetPath(contentRootPath, route.ClusterId)))
            .Select(route => ShutteringPageContentFiles.GetDisplayPath(route.ClusterId))
            .ToArray();

        if (missingContentFiles.Length > 0)
        {
            throw new InvalidOperationException(
                $"The following shuttering content files must exist: {string.Join(", ", missingContentFiles)}."
            );
        }

        return shutteredRoutes;
    }

    private static ShutteredRoute? GetShutteredRoute(IConfigurationSection route)
    {
        var shuttered = route.GetSection("Metadata")[ShutteredMetadataKey];
        if (string.IsNullOrWhiteSpace(shuttered))
        {
            return null;
        }

        if (!bool.TryParse(shuttered, out var isShuttered))
        {
            throw new InvalidOperationException(
                $"The Shuttered metadata for reverse-proxy route '{route.Key}' must be true or false."
            );
        }

        if (!isShuttered)
        {
            return null;
        }

        return new ShutteredRoute(
            route.Key,
            route["ClusterId"] ?? string.Empty,
            route.GetSection("Match")["Path"] ?? string.Empty
        );
    }

    private static void ValidateCluster(ShutteredRoute route, HashSet<string> clusters)
    {
        if (
            string.IsNullOrWhiteSpace(route.ClusterId)
            || !route.ClusterId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            || !clusters.Contains(route.ClusterId)
        )
        {
            throw new InvalidOperationException(
                $"Shuttered reverse-proxy route '{route.RouteId}' must reference an existing cluster with an ID containing only letters, digits, hyphens, or underscores."
            );
        }
    }

    private static void ValidateMatchPath(ShutteredRoute route)
    {
        var matchPath = route.MatchPath;
        var firstSegment = matchPath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var hasUnsafeSegment = matchPath.Split('/').Any(segment => segment is "." or "..");
        var isHealthPath =
            matchPath.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || matchPath.StartsWith("/health/", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(firstSegment)
            || firstSegment.StartsWith('{');

        if (
            string.IsNullOrWhiteSpace(matchPath)
            || !matchPath.StartsWith('/')
            || matchPath.Contains('?', StringComparison.Ordinal)
            || matchPath.Contains('#', StringComparison.Ordinal)
            || matchPath.Contains('\\')
            || (matchPath.Length > 1 && matchPath.EndsWith('/'))
            || hasUnsafeSegment
            || isHealthPath
        )
        {
            throw new InvalidOperationException(
                $"The Match:Path for shuttered reverse-proxy route '{route.RouteId}' must begin with a literal path segment, cannot include health endpoints, and cannot contain . or .. segments."
            );
        }
    }

    private static void ValidateUniqueMatchPaths(IReadOnlyCollection<ShutteredRoute> shutteredRoutes)
    {
        var duplicateMatchPaths = shutteredRoutes
            .GroupBy(route => route.MatchPath, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateMatchPaths.Length > 0)
        {
            throw new InvalidOperationException(
                $"The following shuttered reverse-proxy Match:Path values are duplicated: {string.Join(", ", duplicateMatchPaths)}."
            );
        }
    }
}
