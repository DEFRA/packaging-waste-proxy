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
    public void Validate_WhenShutteringPathIsInvalid_ShouldThrow(string path)
    {
        var configuration = CreateConfiguration(path);

        var act = () =>
            ShutteringConfigurationValidator.Validate(configuration.GetSection("Shuttering"), ContentRootPath);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_WhenShutteringContentFileIsMissing_ShouldThrow()
    {
        var configuration = CreateConfiguration("/missing");

        var act = () =>
            ShutteringConfigurationValidator.Validate(configuration.GetSection("Shuttering"), ContentRootPath);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Shuttering/Pages/missing.html*");
    }

    [Fact]
    public void Validate_WhenShutteringPathAndContentFileAreValid_ShouldNotThrow()
    {
        var configuration = CreateConfiguration("/manage-recycling-obligations");

        var act = () =>
            ShutteringConfigurationValidator.Validate(configuration.GetSection("Shuttering"), ContentRootPath);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("/", "index.html")]
    [InlineData("/manage-recycling-obligations", "manage-recycling-obligations.html")]
    [InlineData("/manage-recycling-obligations/returns", "manage-recycling-obligations/returns.html")]
    public void GetRelativePath_ShouldDeriveHtmlFileFromPath(string path, string expectedFile)
    {
        var relativePath = ShutteringPageContentFiles.GetRelativePath(path);

        relativePath.Should().Be(expectedFile);
    }

    private static string ContentRootPath =>
        Path.GetDirectoryName(typeof(Program).Assembly.Location)
        ?? throw new InvalidOperationException("The ReverseProxy content root could not be found.");

    private static IConfiguration CreateConfiguration(string path) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Shuttering:Paths:0:Path"] = path,
                    ["Shuttering:Paths:0:Shuttered"] = "true",
                }
            )
            .Build();
}
