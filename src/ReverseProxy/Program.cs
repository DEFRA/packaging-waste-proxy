using Defra.PackagingWasteProxy.ReverseProxy.Configuration;
using Defra.PackagingWasteProxy.ReverseProxy.Utils;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Health;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Logging;
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

    builder.Services.AddSingleton<ShutteringPageRenderer>();
    builder.Services.AddReverseProxy().LoadFromConfig(reverseProxyConfiguration);

    var app = builder.Build();

    app.UseHeaderPropagation();
    app.UseGovUkFrontend();
    app.MapShuttering(shutteredRoutes, app.Services.GetRequiredService<ShutteringPageRenderer>());
    app.MapAggregateHealth();
    app.MapReverseProxy();

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
