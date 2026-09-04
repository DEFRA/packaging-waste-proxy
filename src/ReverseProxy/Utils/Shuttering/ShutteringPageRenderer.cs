using System.Text.Encodings.Web;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal sealed class ShutteringPageRenderer(IWebHostEnvironment environment)
{
    private const string PageTitle = "Service Unavailable";

    public async Task Write(HttpContext context, ShutteredRoute route)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";

        if (HttpMethods.IsHead(context.Request.Method))
        {
            return;
        }

        var contentPath = ShutteringPageContentFiles.GetPath(environment.ContentRootPath, route.ClusterId);
        var content = await File.ReadAllTextAsync(contentPath, context.RequestAborted);

        await context.Response.WriteAsync(CreatePage(content), context.RequestAborted);
    }

    private static string CreatePage(string content)
    {
        var pageTitle = HtmlEncoder.Default.Encode(PageTitle);

        return $$"""
            <!DOCTYPE html>
            <html lang="en" class="govuk-template govuk-template--rebranded">
            <head>
              <meta charset="utf-8">
              <title>{{pageTitle}}</title>
              <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
              <meta name="theme-color" content="#1d70b8">
              <link rel="icon" sizes="48x48" href="/assets/images/favicon.ico">
              <link rel="icon" sizes="any" href="/assets/images/favicon.svg" type="image/svg+xml">
              <link rel="mask-icon" href="/assets/images/govuk-icon-mask.svg" color="#1d70b8">
              <link rel="apple-touch-icon" href="/assets/images/govuk-icon-180.png">
              <link rel="manifest" href="/assets/manifest.json">
              <link rel="stylesheet" href="/govuk-frontend.min.css">
            </head>
            <body class="govuk-template__body">
              <a href="#main-content" class="govuk-skip-link">Skip to main content</a>
              <header class="govuk-header">
                <div class="govuk-header__container govuk-width-container">
                  <div class="govuk-header__logo">
                    <a href="https://www.gov.uk/" class="govuk-header__link govuk-header__link--homepage">GOV.UK</a>
                  </div>
                  <div class="govuk-header__content">
                    <span class="govuk-header__service-name">{{pageTitle}}</span>
                  </div>
                </div>
              </header>
              <div class="govuk-width-container">
                <main class="govuk-main-wrapper govuk-main-wrapper--l" id="main-content">
                  <div class="govuk-grid-row">
                    <div class="govuk-grid-column-two-thirds">
            {{content}}
                    </div>
                  </div>
                </main>
              </div>
              <footer class="govuk-footer">
                <div class="govuk-width-container">
                  <div class="govuk-footer__meta">
                    <div class="govuk-footer__meta-item govuk-footer__meta-item--grow">
                      <h2 class="govuk-visually-hidden">Support links</h2>
                      <ul class="govuk-footer__inline-list">
                        <li class="govuk-footer__inline-list-item"><a class="govuk-footer__link" href="https://www.gov.uk/help/privacy-notice">Privacy</a></li>
                        <li class="govuk-footer__inline-list-item"><a class="govuk-footer__link" href="https://www.gov.uk/help/cookies">Cookies</a></li>
                        <li class="govuk-footer__inline-list-item"><a class="govuk-footer__link" href="https://www.gov.uk/help/accessibility-statement">Accessibility statement</a></li>
                      </ul>
                    </div>
                  </div>
                </div>
              </footer>
            </body>
            </html>
            """;
    }
}
