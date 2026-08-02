using System.Globalization;
using Cinedex.Application;
using Cinedex.Application.Configuration;
using Cinedex.Auth.Identity;
using Cinedex.Email.Smtp;
using Cinedex.Persistence.Postgres;
using Cinedex.WebService.Constants;
using Cinedex.WebService.ExceptionHandlers;
using FastEndpoints;

namespace Cinedex.WebService.Extensions;

/// <summary>
/// Extension methods that register the presentation layer's services in the DI container.
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Registers the application, persistence and web-service services (endpoints, OpenAPI,
    /// problem details, health checks and exception handlers) with the DI container.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance so calls can be chained.</returns>
    public static WebApplicationBuilder AddPresentationServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddApplication()
            .AddPersistenceAdapter()
            .AddAuthenticationAdapter()
            .AddEmailAdapter();

        // Bind the SPA base URL used to build user-facing links (e.g. the password-reset link).
        var frontendOptions = new FrontendOptions();
        builder.Configuration.GetSection(FrontendOptions.SectionName).Bind(frontendOptions);
        builder.Services.AddSingleton(frontendOptions);

        // Configure JWT bearer authentication and authorization.
        builder.AddJwtAuthentication();

        // Register FastEndpoints (discovers endpoint classes in this assembly)
        builder.Services.AddFastEndpoints();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();

        // Startup guard: fail immediately if the key is absent altogether, rather than letting it
        // surface later as an unhealthy readiness probe. The value is deliberately not reused below —
        // it cannot be trusted to be the one the application will actually connect with.
        _ = builder.Configuration.GetConnectionString(ConfigurationConstants.DefaultConnection)
            ?? throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Connection string '{0}' is not configured.",
                    ConfigurationConstants.DefaultConnection));

        // Resolved lazily, from the built IServiceProvider, so the probe targets the same connection
        // string AddAuthenticationPersistence hands the DbContexts — a readiness check that passes
        // against a different database than the app uses is worse than no check at all. Reading
        // builder.Configuration eagerly here is what makes them diverge: under
        // WebApplicationFactory the test host's overrides are only layered on once the host finishes
        // building, so an eager read captures appsettings.json's "<SECRETS>" placeholder instead.
        //
        // The read-only check applies the same reasoning to ConnectionStrings:ReadOnlyConnection, with
        // the fallback AddAuthenticationPersistence already uses for AuthReadOnlyDbContext: unset,
        // empty or whitespace means "use the default connection", so the check is always registered —
        // matching AuthReadOnlyDbContext, which always exists whether or not the key is set.
        builder.Services
            .AddHealthChecks()
            .AddNpgSql(
                connectionStringFactory: sp =>
                    sp.GetRequiredService<IConfiguration>()
                        .GetConnectionString(ConfigurationConstants.DefaultConnection)
                    ?? throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Connection string '{0}' is not configured.",
                            ConfigurationConstants.DefaultConnection)),
                name: "postgres",
                tags: [HealthCheckConstants.ReadyTag])
            .AddNpgSql(
                connectionStringFactory: sp =>
                {
                    var configuration = sp.GetRequiredService<IConfiguration>();
                    var readOnlyConnectionString =
                        configuration.GetConnectionString(ConfigurationConstants.ReadOnlyConnection);

                    return string.IsNullOrWhiteSpace(readOnlyConnectionString)
                        ? configuration.GetConnectionString(ConfigurationConstants.DefaultConnection)
                            ?? throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Connection string '{0}' is not configured.",
                                    ConfigurationConstants.DefaultConnection))
                        : readOnlyConnectionString;
                },
                name: "postgres-readonly",
                tags: [HealthCheckConstants.ReadyTag]);

        // Register exception handlers in chain order — DefaultExceptionHandler must be last (catch-all)
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<EntityNotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<InvalidCredentialsExceptionHandler>();
        builder.Services.AddExceptionHandler<DefaultExceptionHandler>();

        return builder;
    }
}