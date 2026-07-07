using Cinedex.Application.Abstractions;
using Cinedex.Persistence.Auth.Identity;
using Cinedex.Persistence.Postgres;
using Cinedex.WebService.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Cinedex.WebService.IntegrationTests;

public class WebApplicationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public HttpClient Client { get; private set; } = null!;

    internal CapturingEmailSender EmailSender => this.Services.GetRequiredService<CapturingEmailSender>();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        this.Client = this.CreateClient();

        using var scope = this.Services.CreateScope();
        var filmDb = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        await filmDb.Database.MigrateAsync();

        await AuthDbInitializer.MigrateAsync(this.Services);
    }

    public new async Task DisposeAsync()
    {
        this.Client.Dispose();
        await base.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Capture password-reset tokens instead of "sending" them.
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<CapturingEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<CapturingEmailSender>());
        });
    }
}
