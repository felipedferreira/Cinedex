using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Movies.Persistence.Postgres;
using Testcontainers.PostgreSql;

namespace Movies.WebService.IntegrationTests;

public class WebApplicationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await this.postgresContainer.StartAsync();

        this.Client = this.CreateClient();

        using var scope = this.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MoviesDbContext>();
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
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MoviesDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<MoviesDbContext>(options =>
                options.UseNpgsql(this.postgresContainer.GetConnectionString()));
        });
    }
}
