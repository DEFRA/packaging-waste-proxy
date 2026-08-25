using AwesomeAssertions;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Health;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests.Utils.Health;

public class WebApplicationExtensionsTests
{
    [Theory]
    [InlineData("health-api-key", "health-api-key", true)]
    [InlineData(null, "health-api-key", false)]
    [InlineData("", "", false)]
    [InlineData("wrong-api-key", "health-api-key", false)]
    [InlineData("short", "longer-api-key", false)]
    public void HasValidApiKey_ShouldRequireExactNonEmptyMatch(
        string? actualApiKey,
        string expectedApiKey,
        bool expectedResult
    )
    {
        var isValid = WebApplicationExtensions.HasValidApiKey(actualApiKey, expectedApiKey);

        isValid.Should().Be(expectedResult);
    }
}
