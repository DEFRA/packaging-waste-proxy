using AwesomeAssertions;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;
using Microsoft.Extensions.Configuration;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests.Shuttering;

public class ShutteringConfigurationValidatorTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/all")]
    [InlineData("manage-recycling-obligations")]
    [InlineData("/manage-recycling-obligations/")]
    [InlineData("/manage-recycling-obligations/../other")]
    [InlineData("/{**catch-all}")]
    public void Validate_WhenShutteredRouteMatchPathIsInvalid_ShouldThrow(string path)
    {
        var configuration = CreateConfiguration(path);

        var act = () =>
            ShutteringConfigurationValidator.Validate(configuration.GetSection("ReverseProxy"), ContentRootPath);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_WhenShutteringContentFileIsMissing_ShouldThrow()
    {
        var configuration = CreateConfiguration("/missing", clusterId: "Missing");

        var act = () =>
            ShutteringConfigurationValidator.Validate(configuration.GetSection("ReverseProxy"), ContentRootPath);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Shuttering/Pages/missing.html*");
    }

    [Fact]
    public void Validate_WhenShutteredRouteAndContentFileAreValid_ShouldNotThrow()
    {
        var configuration = CreateConfiguration("/manage-recycling-obligations");

        var act = () =>
            ShutteringConfigurationValidator.Validate(configuration.GetSection("ReverseProxy"), ContentRootPath);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("ManageRecyclingObligations", "manage-recycling-obligations.html")]
    [InlineData("WastEPR", "wast-epr.html")]
    [InlineData("waste_epr", "waste-epr.html")]
    public void GetRelativePath_ShouldDeriveHtmlFileFromClusterId(string clusterId, string expectedFile)
    {
        var relativePath = ShutteringPageContentFiles.GetRelativePath(clusterId);

        relativePath.Should().Be(expectedFile);
    }

    [Fact]
    public void Validate_WhenRouteIsNotShuttered_ShouldNotRequireContentFile()
    {
        var configuration = CreateConfiguration("/missing", clusterId: "Missing", shuttered: false);

        var act = () =>
            ShutteringConfigurationValidator.Validate(configuration.GetSection("ReverseProxy"), ContentRootPath);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenShutteredMetadataIsNotBoolean_ShouldThrow()
    {
        var configuration = CreateConfiguration("/manage-recycling-obligations", shutteredValue: "yes");

        var act = () =>
            ShutteringConfigurationValidator.Validate(configuration.GetSection("ReverseProxy"), ContentRootPath);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must be true or false*");
    }

    [Fact]
    public void AppSettingsRoutes_ShouldAllHaveHoldingPageContentFiles()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(ContentRootPath, "appsettings.json"))
            .Build();
        var routes = configuration.GetSection("ReverseProxy:Routes").GetChildren().ToArray();
        var missingContentFiles = routes
            .Select(route => new { RouteId = route.Key, ClusterId = route["ClusterId"] })
            .Where(route =>
                string.IsNullOrWhiteSpace(route.ClusterId)
                || !File.Exists(ShutteringPageContentFiles.GetPath(ContentRootPath, route.ClusterId))
            )
            .Select(route => route.RouteId)
            .ToArray();

        routes.Should().NotBeEmpty();
        missingContentFiles.Should().BeEmpty("every configured proxy must be able to be shuttered on deployment");
    }

    private static string ContentRootPath =>
        Path.GetDirectoryName(typeof(Program).Assembly.Location)
        ?? throw new InvalidOperationException("The ReverseProxy content root could not be found.");

    private static IConfiguration CreateConfiguration(
        string path,
        string clusterId = "ManageRecyclingObligations",
        bool shuttered = true,
        string? shutteredValue = null
    ) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ReverseProxy:Routes:Route:ClusterId"] = clusterId,
                    ["ReverseProxy:Routes:Route:Match:Path"] = path,
                    ["ReverseProxy:Routes:Route:Metadata:Shuttered"] = shutteredValue ?? shuttered.ToString(),
                    [$"ReverseProxy:Clusters:{clusterId}:Destinations:Primary:Address"] = "https://example.com/",
                }
            )
            .Build();
}
