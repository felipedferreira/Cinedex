using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Movies.Persistence.Postgres;
using Movies.WebService.IntegrationTests.Constants;
using Testcontainers.PostgreSql;

namespace Movies.WebService.IntegrationTests;

public class WebApplicationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage(TestConfigurationConstants.PostgresImage)
        .Build();

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await this.postgresContainer.StartAsync();

        this.Client = this.CreateClient();

        using var scope = this.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        this.Client.Dispose();
        await base.DisposeAsync();
        await this.postgresContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [TestConfigurationConstants.DefaultConnectionPath] = this.postgresContainer.GetConnectionString(),
            });
        });
    }
}
