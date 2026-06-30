using Cinedex.WebService.Constants;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cinedex.WebService.Extensions;

/// <summary>
/// Extension methods that wire up OpenTelemetry logging and tracing.
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
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance so calls can be chained.</returns>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var otlpEndpointConfigured =
            !string.IsNullOrWhiteSpace(builder.Configuration[ConfigurationConstants.OtlpEndpoint]);

        var serviceName = builder.Configuration[ConfigurationConstants.OtelServiceName] ?? "Cinedex.WebService";

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Npgsql"); // EF Core/Npgsql emit DB spans on this ActivitySource

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