using System.Net;
using AwesomeAssertions;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests.Shuttering;

[Collection(nameof(WebApplicationFactoryCollection))]
public class ShutteringTests(ShutteredReverseProxyWebApplicationFactory factory)
    : IClassFixture<ShutteredReverseProxyWebApplicationFactory>
{
    [Fact]
    public async Task RequestToShutteredPath_ShouldReturnHoldingPageWithConfiguredHtmlBody()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/manage-recycling-obligations/returns",
            new StringContent("{}"),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        response.Headers.CacheControl!.ToString().Should().Be("no-store");

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        content.Should().Contain("<title>Service Unavailable</title>");
        content.Should().Contain("<h1 class=\"govuk-heading-l\">Sorry, the service is unavailable</h1>");
        content.Should().Contain("https://www.gov.uk/guidance/contact-defra");
        content.Should().Contain("/govuk-frontend.min.css");
    }

    [Fact]
    public async Task RequestToGovUkStylesheet_ShouldReturnStylesheet()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/govuk-frontend.min.css", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/css");
    }

    [Fact]
    public async Task RequestToHealth_ShouldNotBeShuttered()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
