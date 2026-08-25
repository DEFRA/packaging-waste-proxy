using Microsoft.AspNetCore.Mvc.Testing;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests;

public sealed class ReverseProxyWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string DestinationAddressEnvironmentVariable =
        "ReverseProxy__Clusters__ManageRecyclingObligations__Destinations__Primary__Address";
    private const string HealthAllApiKeyEnvironmentVariable = "Health__All__ApiKey";
    private const string PortEnvironmentVariable = "PORT";

    private readonly string? _previousDestinationAddress;
    private readonly string? _previousHealthAllApiKey;
    private readonly string? _previousPort;

    public ReverseProxyWebApplicationFactory()
        : this("https://manage-recycling-obligations.example/") { }

    internal ReverseProxyWebApplicationFactory(
        string destinationAddress,
        string? port = null,
        string? healthAllApiKey = null
    )
    {
        _previousDestinationAddress = Environment.GetEnvironmentVariable(DestinationAddressEnvironmentVariable);
        _previousHealthAllApiKey = Environment.GetEnvironmentVariable(HealthAllApiKeyEnvironmentVariable);
        _previousPort = Environment.GetEnvironmentVariable(PortEnvironmentVariable);
        Environment.SetEnvironmentVariable(DestinationAddressEnvironmentVariable, destinationAddress);
        Environment.SetEnvironmentVariable(HealthAllApiKeyEnvironmentVariable, healthAllApiKey);
        Environment.SetEnvironmentVariable(PortEnvironmentVariable, port);
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable(DestinationAddressEnvironmentVariable, _previousDestinationAddress);
        Environment.SetEnvironmentVariable(HealthAllApiKeyEnvironmentVariable, _previousHealthAllApiKey);
        Environment.SetEnvironmentVariable(PortEnvironmentVariable, _previousPort);

        base.Dispose(disposing);
    }
}
