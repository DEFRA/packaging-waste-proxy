using System.Net;
using AwesomeAssertions;

namespace Defra.PackagingWasteProxy.ReverseProxy.Tests;

[Collection(nameof(WebApplicationFactoryCollection))]
public class NotFoundTests(ReverseProxyWebApplicationFactory factory) : IClassFixture<ReverseProxyWebApplicationFactory>
{
    [Theory]
    [InlineData("/not-permitted")]
    [InlineData("/not-permitted/a-nested-path")]
    public async Task RequestToUnconfiguredPath_ShouldReturnPageNotFound(string path)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd("text/html");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        response.Headers.CacheControl!.ToString().Should().Be("no-store");

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        content.Should().Contain("<title>Page not found</title>");
        content.Should().Contain("<h1 class=\"govuk-heading-l\">Page not found</h1>");
        content.Should().Contain("<div class=\"govuk-body\">");
        content.Should().Contain("/govuk-frontend.min.css");
        content.Should().Contain("class=\"govuk-header__logotype\"");
        content.Should().Contain("class=\"govuk-footer__crown\"");
        content.Should().Contain("class=\"govuk-footer__licence-logo\"");
        content.Should().Contain("Open Government Licence v3.0");
        content.Should().Contain("© Crown copyright");
    }

    [Fact]
    public async Task PostRequestAcceptingHtmlToUnconfiguredPath_ShouldReturnPageNotFound()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/not-permitted")
        {
            Content = new StringContent("{}"),
        };
        request.Headers.Accept.ParseAdd("text/html");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Theory]
    [InlineData("/not-permitted/application.css", "text/css")]
    [InlineData("/not-permitted/font.woff2", "*/*")]
    public async Task AssetRequestToUnconfiguredPath_ShouldReturnEmptyNotFound(string path, string accept)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd(accept);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.Should().BeNull();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        content.Should().BeEmpty();
    }
}
