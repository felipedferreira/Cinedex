using Cinedex.Auth.Identity;
using Cinedex.Observability.OpenTelemetry.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cinedex.SchedulerWorker;

public sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("application.json", optional: false, reloadOnChange: false)
            .AddJsonFile(
                $"application.{builder.Environment.EnvironmentName}.json",
                optional: true,
                reloadOnChange: false);

        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets<Program>(optional: true);
        }

        // Re-add high-precedence sources after the worker-specific JSON files.
        builder.Configuration
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        builder.AddObservability(
            defaultServiceName: "Cinedex.SchedulerWorker",
            configureTracing: tracing => tracing.AddSource("Npgsql"));

        builder.Services.AddAuthenticationPersistence();
        builder.Services.AddRefreshTokenCleanup();

        // Read from the descriptors rather than resolving IHostedService: resolving would construct
        // the workers here, and a worker whose options fail ValidateOnStart would then throw outside
        // the try below — losing the very log line that explains the failure.
        var jobs = builder.Services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .Select(descriptor => descriptor.ImplementationType?.Name)
            .Where(name => name is not null)
            .ToArray();

        using IHost host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        var telemetryExport = string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"])
            ? "disabled (no OTLP endpoint configured)"
            : "enabled";

        logger.LogInformation(
            "Cinedex scheduler worker starting. Environment {Environment}, telemetry export {TelemetryExport}, {JobCount} scheduled job(s): {Jobs}.",
            builder.Environment.EnvironmentName,
            telemetryExport,
            jobs.Length,
            string.Join(", ", jobs));

        try
        {
            await host.RunAsync();
            return 0;
        }
        catch (Exception exception)
        {
            // Without this the process exits 1 with the reason visible only in the framework's own
            // "Hosting failed to start" line — and never reaches Seq if startup died before the
            // exporter was running.
            logger.LogCritical(exception, "Cinedex scheduler worker terminated unexpectedly.");
            return 1;
        }
    }
}
