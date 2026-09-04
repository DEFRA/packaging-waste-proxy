namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal sealed class ShutteringOptions
{
    public const string SectionName = "Shuttering";

    public List<ShutteringPageOptions> Paths { get; init; } = [];
}

internal sealed class ShutteringPageOptions
{
    public string? Path { get; init; }

    public bool Shuttered { get; init; }
}
