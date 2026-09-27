using System.Diagnostics;
using FoundryOceanus.Observability.OpenTelemetry.Configuration;
using FoundryOceanus.Observability.OpenTelemetry.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace FoundryOceanus.Observability.OpenTelemetry.Hosting;

/// <summary>
/// Reports the effective OTLP export once at startup, and flushes pending logs and traces once every hosted
/// service has stopped.
/// </summary>
/// <remarks>
/// The flush works around the logs exporter in OpenTelemetry 1.16: over http/protobuf it creates its
/// HttpClient lazily, on its first export, by resolving IHttpClientFactory from the service provider. When that
/// first export is the final flush during container disposal (any host that stops within one batch interval,
/// such as the database migrator) the provider is already disposed and the batch is dropped.
/// <see cref="StoppedAsync"/> runs after every hosted service, Kestrel included, has stopped and before the
/// container is disposed, so the flush happens while the provider is still alive. Metrics are not flushed here:
/// their exporter creates its client eagerly and the meter provider exports a final collection when disposed.
/// </remarks>
/// <param name="settings">The export settings resolved by <c>AddObservability</c>.</param>
/// <param name="configuration">The final host configuration, used to report OTEL_SDK_DISABLED.</param>
/// <param name="services">The root service provider, used to reach the tracer and logger providers.</param>
/// <param name="logger">Logger for the startup line and configuration warnings.</param>
internal sealed partial class ObservabilityLifecycleService(
    OtlpExportSettings settings,
    IConfiguration configuration,
    IServiceProvider services,
    ILogger<ObservabilityLifecycleService> logger) : IHostedLifecycleService
{
    private const int FlushBudgetMilliseconds = 5000;

    /// <inheritdoc/>
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        if (IsSdkDisabled(configuration))
        {
            LogExportDisabledBySdk(logger);
            return Task.CompletedTask;
        }

        LogExport(logger, settings.Describe());

        foreach (string warning in settings.Warnings)
        {
            LogConfigurationWarning(logger, warning);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        if (!settings.IsEnabled)
        {
            return Task.CompletedTask;
        }

        Stopwatch elapsed = Stopwatch.StartNew();

        // Logs first: they are the signal the lazy HttpClient loses. Both calls share one budget, kept under
        // Docker's 10 s stop grace period.
        services.GetService<LoggerProvider>()?.ForceFlush(RemainingMilliseconds(elapsed));
        services.GetService<TracerProvider>()?.ForceFlush(RemainingMilliseconds(elapsed));

        return Task.CompletedTask;
    }

    private static int RemainingMilliseconds(Stopwatch elapsed) =>
        (int)Math.Max(0L, FlushBudgetMilliseconds - elapsed.ElapsedMilliseconds);

    private static bool IsSdkDisabled(IConfiguration source) =>
        bool.TryParse(source[ConfigurationConstants.SdkDisabled]?.Trim(), out bool disabled) && disabled;

    [LoggerMessage(Level = LogLevel.Information, Message = "OpenTelemetry OTLP export: {Export}.")]
    private static partial void LogExport(ILogger log, string export);

    [LoggerMessage(Level = LogLevel.Information, Message = "OpenTelemetry OTLP export: disabled (OTEL_SDK_DISABLED=true).")]
    private static partial void LogExportDisabledBySdk(ILogger log);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OpenTelemetry OTLP configuration: {Warning}")]
    private static partial void LogConfigurationWarning(ILogger log, string warning);
}