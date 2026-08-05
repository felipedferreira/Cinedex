using FoundryOceanus.Observability.OpenTelemetry.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FoundryOceanus.Observability.OpenTelemetry.Extensions;

/// <summary>
/// Extension methods that wire up OpenTelemetry logging and tracing for any .NET host.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Configures OpenTelemetry logs and traces, exported to Seq over OTLP.
    /// The OTLP endpoint, protocol and the Seq API-key header are supplied via the
    /// standard OTEL_EXPORTER_OTLP_* environment variables (see compose.yaml). When no
    /// endpoint is configured (local <c>dotnet run</c>, integration tests) the exporters are
    /// omitted so nothing tries to reach a Seq that isn't there.
    /// </summary>
    /// <param name="builder">
    /// The host builder to configure. Both <c>HostApplicationBuilder</c> (console hosts) and
    /// <c>WebApplicationBuilder</c> implement <see cref="IHostApplicationBuilder"/>.
    /// </param>
    /// <param name="defaultServiceName">
    /// Service name to report when OTEL_SERVICE_NAME is not set. Defaults to the host's
    /// application name.
    /// </param>
    /// <param name="configureTracing">
    /// Host-specific tracing instrumentation, e.g. <c>AddAspNetCoreInstrumentation()</c> for a
    /// web host or <c>AddSource("Npgsql")</c> for a host that talks to the database.
    /// </param>
    /// <returns>The same <paramref name="builder"/> instance so calls can be chained.</returns>
    public static IHostApplicationBuilder AddObservability(
        this IHostApplicationBuilder builder,
        string? defaultServiceName = null,
        Action<TracerProviderBuilder>? configureTracing = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var otlpEndpointConfigured =
            !string.IsNullOrWhiteSpace(builder.Configuration[ConfigurationConstants.OtlpEndpoint]);

        var serviceName = builder.Configuration[ConfigurationConstants.OtelServiceName]
            ?? defaultServiceName
            ?? builder.Environment.ApplicationName;

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                configureTracing?.Invoke(tracing);

                if (otlpEndpointConfigured)
                {
                    tracing.AddOtlpExporter();
                }
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));

            if (otlpEndpointConfigured)
            {
                logging.AddOtlpExporter();
            }
        });

        return builder;
    }
}
