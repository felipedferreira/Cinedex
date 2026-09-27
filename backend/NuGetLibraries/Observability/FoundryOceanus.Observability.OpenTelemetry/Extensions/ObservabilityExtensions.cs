using FoundryOceanus.Observability.OpenTelemetry.Configuration;
using FoundryOceanus.Observability.OpenTelemetry.Constants;
using FoundryOceanus.Observability.OpenTelemetry.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FoundryOceanus.Observability.OpenTelemetry.Extensions;

/// <summary>
/// Extension methods that wire up OpenTelemetry tracing, metrics and logging for any .NET host.
/// </summary>
public static class ObservabilityExtensions
{
    private const string RuntimeMeterName = "System.Runtime";

    /// <summary>
    /// Configures OpenTelemetry traces, metrics and logs, exported over OTLP to any backend (Seq, SigNoz, the
    /// Aspire dashboard, a collector). Everything is driven by the standard OTEL_* keys, read through
    /// <see cref="IHostApplicationBuilder.Configuration"/> with the host's usual precedence, so environment
    /// variables, appsettings/application.json, user secrets and the command line all work. The transport is
    /// OTEL_EXPORTER_OTLP_PROTOCOL: "http/protobuf" (the default when unset, unlike the SDK's own gRPC default)
    /// or "grpc". When no endpoint is configured (local <c>dotnet run</c>, tests) the exporters are omitted so
    /// nothing tries to reach a backend that isn't there. Call this after every configuration source has been added.
    /// </summary>
    /// <param name="builder">
    /// The host builder to configure. Both <c>HostApplicationBuilder</c> (console hosts) and
    /// <c>WebApplicationBuilder</c> implement <see cref="IHostApplicationBuilder"/>.
    /// </param>
    /// <param name="defaultServiceName">
    /// Service name to report when OTEL_SERVICE_NAME is not set. Defaults to the host's application name.
    /// </param>
    /// <param name="configureTracing">
    /// Host-specific tracing instrumentation, e.g. <c>AddAspNetCoreInstrumentation()</c> for a web host or
    /// <c>AddSource("Npgsql")</c> for a host that talks to the database.
    /// </param>
    /// <param name="configureMetrics">
    /// Host-specific meters and instrumentation, e.g. <c>AddAspNetCoreInstrumentation()</c> or
    /// <c>AddMeter("Npgsql")</c>. The .NET runtime meter ("System.Runtime") is always added.
    /// </param>
    /// <returns>The same <paramref name="builder"/> instance so calls can be chained.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this method has already been called on <paramref name="builder"/>, or when an endpoint is
    /// configured and either it is not an absolute http:// or https:// URL or the protocol is neither "grpc"
    /// nor "http/protobuf".
    /// </exception>
    public static IHostApplicationBuilder AddObservability(
        this IHostApplicationBuilder builder,
        string? defaultServiceName = null,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(OtlpExportSettings)))
        {
            throw new InvalidOperationException(
                $"{nameof(AddObservability)} has already been called on this builder; a second call would add a second OTLP exporter per signal.");
        }

        // Resolved once from every source added so far; throws on a malformed endpoint or protocol so a typo
        // fails the deploy instead of the SDK silently falling back to localhost or gRPC.
        OtlpExportSettings export = OtlpExportSettings.Resolve(builder.Configuration);

        string serviceName = builder.Configuration[ConfigurationConstants.OtelServiceName]
            ?? defaultServiceName
            ?? builder.Environment.ApplicationName;

        builder.Services.AddSingleton(export);
        builder.Services.AddHostedService<ObservabilityLifecycleService>();

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                configureTracing?.Invoke(tracing);

                if (export.ExportTraces)
                {
                    tracing.AddOtlpExporter(export.Apply);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(RuntimeMeterName);
                configureMetrics?.Invoke(metrics);

                if (export.ExportMetrics)
                {
                    metrics.AddOtlpExporter(export.Apply);
                }
            })
            .WithLogging(
                logging =>
                {
                    if (export.ExportLogs)
                    {
                        logging.AddOtlpExporter(export.Apply);
                    }
                },
                options =>
                {
                    options.IncludeScopes = true;
                    options.IncludeFormattedMessage = true;
                });

        return builder;
    }
}