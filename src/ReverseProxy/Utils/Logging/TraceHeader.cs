using System.ComponentModel.DataAnnotations;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Logging;

public class TraceHeader
{
    [ConfigurationKeyName("TraceHeader")]
    [Required]
    public required string Name { get; set; }
}
