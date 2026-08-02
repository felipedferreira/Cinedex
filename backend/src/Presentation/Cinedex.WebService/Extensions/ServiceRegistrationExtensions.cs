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

        var connectionString = builder.Configuration.GetConnectionString(ConfigurationConstants.DefaultConnection)
            ?? throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Connection string '{0}' is not configured.",
                    ConfigurationConstants.DefaultConnection));

        // The read-only check resolves ConnectionStrings:ReadOnlyConnection lazily, through the built
        // IServiceProvider, rather than eagerly like DefaultConnection above. A WebApplicationFactory
        // test host layers its configuration overrides on only once the host finishes building; an
        // eager read here would run before that and silently miss them. Real deployments are
        // unaffected — environment variables are visible eagerly there — but resolving this the same
        // way AddAuthenticationPersistence resolves it for AuthReadOnlyDbContext keeps the two
        // consistent and keeps this testable. The fallback mirrors that same method: unset, empty or
        // whitespace means "use the default connection", so the check is always registered — matching
        // AuthReadOnlyDbContext, which always exists whether or not the key is set.
        builder.Services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString: connectionString,
                name: "postgres",
                tags: [HealthCheckConstants.ReadyTag])
            .AddNpgSql(
                connectionStringFactory: sp =>
                {
                    var configuration = sp.GetRequiredService<IConfiguration>();
                    var readOnlyConnectionString =
                        configuration.GetConnectionString(ConfigurationConstants.ReadOnlyConnection);

                    return string.IsNullOrWhiteSpace(readOnlyConnectionString)
                        ? configuration.GetConnectionString(ConfigurationConstants.DefaultConnection) ?? connectionString
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