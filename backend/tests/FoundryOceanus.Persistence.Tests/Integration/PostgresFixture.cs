using FoundryOceanus.Persistence.EntityFrameworkCore.Postgres.DependencyInjection;
using FoundryOceanus.Persistence.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FoundryOceanus.Persistence.Tests.Integration;

/// <summary>
/// Starts one PostgreSQL container for the whole integration collection and builds service providers
/// against it.
/// </summary>
/// <remarks>
/// Real PostgreSQL rather than the in-memory provider, and not optionally. Everything these tests
/// cover â€” SQLSTATE classification, serialization failures, advisory locks, savepoints â€” either does
/// not exist in the in-memory provider or behaves differently there. A test suite that passed against
/// a fake would be asserting that the fake works.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using ServiceProvider provider = BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Builds a provider wired the way a consuming application would wire it.
    /// </summary>
    /// <param name="useDetachedRepository">
    /// Registers the repository that holds its own context, to exercise the shared-context check.
    /// </param>
    /// <returns>The service provider. The caller owns it.</returns>
    public ServiceProvider BuildServiceProvider(bool useDetachedRepository = false)
    {
        var services = new ServiceCollection();

        services.AddDbContext<WidgetDbContext>(options => options.UseNpgsql(ConnectionString));

        if (useDetachedRepository)
        {
            services.AddDbContextFactory<WidgetDbContext>(
                options => options.UseNpgsql(ConnectionString),
                lifetime: ServiceLifetime.Singleton);
        }

        services.AddNpgsqlUnitOfWork<WidgetDbContext>(uow =>
        {
            if (useDetachedRepository)
            {
                uow.AddRepository<IWidgetRepository, DetachedWidgetRepository>();
            }
            else
            {
                uow.AddRepository<IWidgetRepository, WidgetRepository>();
            }
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Empties the table, for tests whose assertions depend on the total row count.
    /// </summary>
    /// <returns>A task that completes when the table is empty.</returns>
    public async Task ResetAsync()
    {
        await using ServiceProvider provider = BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE widgets;");
    }
}
