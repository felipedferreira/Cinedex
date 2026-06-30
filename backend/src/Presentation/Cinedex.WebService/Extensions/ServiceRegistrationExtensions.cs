using System.Globalization;
using Cinedex.Application;
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
            .AddPersistenceAdapter();

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

        builder.Services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString: connectionString,
                name: "postgres",
                tags: [HealthCheckConstants.ReadyTag]);

        // Register exception handlers in chain order — DefaultExceptionHandler must be last (catch-all)
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<EntityNotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<DefaultExceptionHandler>();

        return builder;
    }
}
