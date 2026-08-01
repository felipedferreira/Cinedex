using Cinedex.Auth.Identity;
using Cinedex.Observability.OpenTelemetry.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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

        using IHost host = builder.Build();

        try
        {
            await host.RunAsync();
            return 0;
        }
        catch (Exception)
        {
            return 1;
        }
    }
}
