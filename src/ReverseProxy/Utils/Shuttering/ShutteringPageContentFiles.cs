namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal static class ShutteringPageContentFiles
{
    private const string ContentDirectory = "Shuttering/Pages";

    public static string GetPath(string contentRootPath, string clusterId) =>
        Path.Combine(contentRootPath, ContentDirectory, GetRelativePath(clusterId));

    public static string GetRelativePath(string clusterId) => $"{ToKebabCase(clusterId)}.html";

    public static string GetDisplayPath(string clusterId) => $"{ContentDirectory}/{GetRelativePath(clusterId)}";

    private static string ToKebabCase(string value)
    {
        var characters = new List<char>(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            var previousCharacter = index > 0 ? value[index - 1] : '\0';
            var nextCharacter = index < value.Length - 1 ? value[index + 1] : '\0';

            if (
                index > 0
                && char.IsUpper(character)
                && (char.IsLower(previousCharacter) || char.IsDigit(previousCharacter) || char.IsLower(nextCharacter))
            )
            {
                characters.Add('-');
            }

            characters.Add(character == '_' ? '-' : char.ToLowerInvariant(character));
        }

        return new string([.. characters]);
    }
}
