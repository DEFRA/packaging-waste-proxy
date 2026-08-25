using System.ComponentModel.DataAnnotations;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Health;

public sealed class HealthAllOptions
{
    public const string SectionName = "Health:All";

    public string ApiKey { get; init; } = "";

    [Range(1, int.MaxValue)]
    public int DownstreamTimeoutMilliseconds { get; init; } = 5000;
}
