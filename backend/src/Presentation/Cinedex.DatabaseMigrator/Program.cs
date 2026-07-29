using Cinedex.Auth.Identity;
using Cinedex.Persistence.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinedex.DatabaseMigrator;

public sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, configuration) =>
            {
                configuration
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("application.json", optional: false, reloadOnChange: false)
                    .AddJsonFile(
                        $"application.{context.HostingEnvironment.EnvironmentName}.json",
                        optional: true,
                        reloadOnChange: false);

                if (context.HostingEnvironment.IsDevelopment())
                {
                    configuration.AddUserSecrets<Program>(optional: true);
                }

                // Re-add high-precedence sources after the migrator-specific JSON files.
                configuration
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            })
            .ConfigureServices(services =>
            {
                services
                    .AddPersistenceAdapter()
                    .AddAuthenticationPersistence();

                services.AddHostedService<DatabaseMigrationHostedService>();
            })
            .Build();

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
