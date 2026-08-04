using Cinedex.Auth.Identity;
using Cinedex.Persistence.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FoundryOceanus.Observability.OpenTelemetry.Extensions;

namespace Cinedex.DatabaseMigrator;

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

        // Re-add high-precedence sources after the migrator-specific JSON files.
        builder.Configuration
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        builder.AddObservability(
            defaultServiceName: "Cinedex.DatabaseMigrator",
            configureTracing: tracing => tracing.AddSource("Npgsql"));

        builder.Services
            .AddPersistenceAdapter()
            .AddAuthenticationPersistence();

        builder.Services.AddHostedService<DatabaseMigrationHostedService>();

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
