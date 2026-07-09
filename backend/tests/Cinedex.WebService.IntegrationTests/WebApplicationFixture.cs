using Cinedex.Application.Abstractions;
using Cinedex.Auth.Identity;
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

    /// <summary>
    /// Gets a client that does not maintain a cookie jar, for tests that must present a specific
    /// refresh-token cookie rather than whatever a previous request happened to leave behind.
    /// </summary>
    public HttpClient CookielessClient { get; private set; } = null!;

    internal CapturingEmailSender EmailSender => this.Services.GetRequiredService<CapturingEmailSender>();

    internal string ConnectionString => _postgresContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // The refresh-token cookie is Secure, and CookieContainer refuses to send a Secure cookie
        // over http://. TestServer performs no real TLS; this only makes Request.IsHttps true.
        this.Client = this.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        this.CookielessClient = this.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
        });

        using var scope = this.Services.CreateScope();
        var filmDb = scope.ServiceProvider.GetRequiredService<FilmDbContext>();
        await filmDb.Database.MigrateAsync();

        await AuthDbInitializer.MigrateAsync(this.Services);
    }

    public new async Task DisposeAsync()
    {
        this.Client.Dispose();
        this.CookielessClient.Dispose();
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
