using System.Diagnostics.CodeAnalysis;

namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Metrics;

[ExcludeFromCodeCoverage]
public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCloudWatchMetrics(this IApplicationBuilder builder)
    {
        var configuration = builder.ApplicationServices.GetRequiredService<IConfiguration>();
        var enabled = configuration.GetValue("AWS_EMF_ENABLED", true);

        if (!enabled)
            return builder;

        var awsNamespace = configuration.GetValue<string>("AWS_EMF_NAMESPACE");
        var environment = configuration.GetValue<string>("AWS_EMF_ENVIRONMENT") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(awsNamespace) && environment.Equals("Local"))
            awsNamespace = typeof(Program).Namespace ?? nameof(Program);

        if (string.IsNullOrWhiteSpace(awsNamespace))
            throw new InvalidOperationException("AWS_EMF_NAMESPACE is not set but metrics are enabled");

        MetricsExporter.Init(builder.ApplicationServices.GetRequiredService<ILoggerFactory>(), awsNamespace);

        return builder;
    }
}
