using Cinedex.Auth.Identity;
using Cinedex.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cinedex.DatabaseMigrator;

internal sealed class DatabaseMigrationHostedService(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime applicationLifetime,
    ILogger<DatabaseMigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Applying catalog database migrations.");

            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            var filmDbContext = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
            await filmDbContext.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Applying authentication database migrations.");
            await AuthDbInitializer.MigrateAsync(scope.ServiceProvider, cancellationToken);

            logger.LogInformation("All database migrations were applied successfully.");
        }
        finally
        {
            applicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
