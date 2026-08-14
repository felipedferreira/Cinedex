using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cinedex.Application.Abstractions;
using Cinedex.Auth.Identity;
using Cinedex.Persistence.Postgres;
using Cinedex.WebService.IntegrationTests.Constants;
using Cinedex.WebService.IntegrationTests.Fakes;
using FoundryOceanus.WebService.Contracts.Requests;
using FoundryOceanus.WebService.Contracts.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Cinedex.WebService.IntegrationTests;

public class WebApplicationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Gets a client that does not maintain a cookie jar, for tests that must present a specific
    /// refresh-token cookie rather than whatever a previous request happened to leave behind.
    /// </summary>
    public HttpClient CookielessClient { get; private set; } = null!;

    /// <summary>
    /// Gets a client whose default <c>Authorization</c> header carries a bearer token for a
    /// dedicated fixture user. The catalog is members-only, so catalog tests use this client.
    /// </summary>
    public HttpClient AuthenticatedClient { get; private set; } = null!;

    internal CapturingEmailSender EmailSender => this.Services.GetRequiredService<CapturingEmailSender>();

    internal CapturingLoggerProvider LoggerProvider { get; } = new();

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

        // Registration needs the seeded roles, so the fixture user is created after migrations.
        // The 15-minute access-token lifetime comfortably covers a test run.
        this.AuthenticatedClient = this.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
        });
        this.AuthenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await CreateFixtureUserAccessTokenAsync());
    }

    public new async Task DisposeAsync()
    {
        this.Client.Dispose();
        this.CookielessClient.Dispose();
        this.AuthenticatedClient.Dispose();
        await base.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddProvider(this.LoggerProvider));

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
                ["Smtp:Host"] = "localhost",
                ["Smtp:Port"] = "1025",
                ["Smtp:Username"] = "cinedex-tests",
                ["Smtp:Password"] = "cinedex-tests-password",
                ["Smtp:FromAddress"] = "no-reply@cinedex.test",
                ["Smtp:FromName"] = "Cinedex Tests",
                ["Smtp:SecureSocketOptions"] = "None",
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

    private async Task<string> CreateFixtureUserAccessTokenAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"fixture-{suffix}@example.com";
        const string password = "F1xture@Pass!";

        var register = await this.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.RegisterEndpoint,
            new RegisterRequest { Email = email, Username = $"fixture{suffix}", Password = password });
        register.EnsureSuccessStatusCode();

        var login = await this.CookielessClient.PostAsJsonAsync(
            TestRouteConstants.Auth.LoginEndpoint,
            new LoginRequest { Email = email, Password = password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Login returned an empty body.");
        return body.AccessToken;
    }
}
