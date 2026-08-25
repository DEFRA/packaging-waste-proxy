using Defra.PackagingWasteProxy.ReverseProxy.Configuration;
using Defra.PackagingWasteProxy.ReverseProxy.Utils;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Health;
using Defra.PackagingWasteProxy.ReverseProxy.Utils.Logging;
using Elastic.CommonSchema.Serilog;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(new EcsTextFormatter()).CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables();
    builder.Services.AddCustomTrustStore();
    builder.ConfigureLoggingAndTracing();
    builder.Services.AddAggregateHealth(builder.Configuration);

    var port = builder.Configuration["PORT"];
    if (int.TryParse(port, out var configuredPort))
    {
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(configuredPort));
    }

    var reverseProxyConfiguration = builder.Configuration.GetSection("ReverseProxy");
    ReverseProxyConfigurationValidator.Validate(reverseProxyConfiguration);
    builder.Services.AddReverseProxy().LoadFromConfig(reverseProxyConfiguration);

    var app = builder.Build();

    app.UseHeaderPropagation();
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
