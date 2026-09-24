using Defra.PackagingWasteProxy.ReverseProxy.Configuration;
using Defra.PackagingWasteProxy.ReverseProxy.Utils;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Health;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Logging;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Pages;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;
using Elastic.CommonSchema.Serilog;
using GovUk.Frontend.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(new EcsTextFormatter()).CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables();
    builder.Services.AddCustomTrustStore();
    builder.ConfigureLoggingAndTracing();
    builder.Services.AddAggregateHealth(builder.Configuration);
    builder.Services.AddGovUkFrontend(options =>
        options.FrontendPackageHostingOptions =
            FrontendPackageHostingOptions.HostAssets
            | FrontendPackageHostingOptions.HostCompiledFiles
            | FrontendPackageHostingOptions.RemoveSourceMapReferences
    );

    var port = builder.Configuration["PORT"];
    if (int.TryParse(port, out var configuredPort))
    {
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(configuredPort));
    }

    var reverseProxyConfiguration = builder.Configuration.GetSection("ReverseProxy");
    ReverseProxyConfigurationValidator.Validate(reverseProxyConfiguration);
    var shutteredRoutes = ShutteringConfigurationValidator.Validate(
        reverseProxyConfiguration,
        builder.Environment.ContentRootPath
    );

    builder.Services.AddSingleton<PageRenderer>();
    builder.Services.AddShutteringMetrics();
    builder
        .Services.AddReverseProxy()
        .LoadFromConfig(reverseProxyConfiguration)
        .AddDnsDestinationResolver(options => options.RefreshPeriod = TimeSpan.FromSeconds(60));

    var app = builder.Build();

    var pageRenderer = app.Services.GetRequiredService<PageRenderer>();
    var shutteredPages = shutteredRoutes
        .Select(route => new ShutteredPage(
            route.RouteId,
            route.MatchPath,
            pageRenderer.Load(
                "Service Unavailable",
                ShutteringPageContentFiles.GetPath(app.Environment.ContentRootPath, route.ClusterId)
            )
        ))
        .ToArray();
    var notFoundPage = new NotFoundPage(
        pageRenderer.Load("Page not found", Path.Combine(app.Environment.ContentRootPath, "Pages", "not-found.html"))
    );

    app.UseHeaderPropagation();
    app.UseGovUkFrontend();
    app.UseCloudWatchMetrics();
    app.MapShuttering(shutteredPages);
    app.MapAggregateHealth();
    app.MapReverseProxy();
    app.MapFallback(notFoundPage.Write).WithDisplayName("Not found");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");

    throw new InvalidOperationException("Application start-up failed.", ex);
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program
{
    protected Program() { }
}
