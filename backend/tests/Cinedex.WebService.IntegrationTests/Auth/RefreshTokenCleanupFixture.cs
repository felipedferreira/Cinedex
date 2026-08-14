using Cinedex.Auth.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Cinedex.WebService.IntegrationTests.Auth;

/// <summary>
/// Owns a Postgres container for the refresh-token retention tests.
/// </summary>
/// <remarks>
/// Deliberately separate from <c>WebApplicationFixture</c>. The sweep under test deletes
/// refresh-token rows, and the endpoint tests sharing that fixture assert exact row counts within a
/// token family — running both against one database would make those assertions race.
/// </remarks>
public sealed class RefreshTokenCleanupFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    internal string ConnectionString => this._postgresContainer.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await this._postgresContainer.StartAsync();

        await using var provider = BuildProvider(this.ConnectionString);
        await AuthDbInitializer.MigrateAsync(provider);
    }

    public async ValueTask DisposeAsync()
    {
        await this._postgresContainer.DisposeAsync();
    }

    // Resolves AuthDbContext without a web host, the way SmtpEmailSenderTests resolves the email
    // adapter: hand-rolled provider over the adapter's own public registration.
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddAuthenticationPersistence()
            .BuildServiceProvider();
    }
}
