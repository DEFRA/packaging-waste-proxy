using Microsoft.AspNetCore.Mvc.Testing;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests;

public sealed class ReverseProxyWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string DestinationAddressEnvironmentVariable =
        "ReverseProxy__Clusters__ManageRecyclingObligations__Destinations__Primary__Address";
    private const string PortEnvironmentVariable = "PORT";

    private readonly string? _previousDestinationAddress;
    private readonly string? _previousPort;

    public ReverseProxyWebApplicationFactory()
        : this("https://manage-recycling-obligations.example/") { }

    internal ReverseProxyWebApplicationFactory(string destinationAddress, string? port = null)
    {
        _previousDestinationAddress = Environment.GetEnvironmentVariable(DestinationAddressEnvironmentVariable);
        _previousPort = Environment.GetEnvironmentVariable(PortEnvironmentVariable);
        Environment.SetEnvironmentVariable(DestinationAddressEnvironmentVariable, destinationAddress);
        Environment.SetEnvironmentVariable(PortEnvironmentVariable, port);
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable(DestinationAddressEnvironmentVariable, _previousDestinationAddress);
        Environment.SetEnvironmentVariable(PortEnvironmentVariable, _previousPort);

        base.Dispose(disposing);
    }
}
