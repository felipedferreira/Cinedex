using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Movies.WebService.Constants;
using Scalar.AspNetCore;

namespace Movies.WebService.Extensions;

/// <summary>
/// Extension methods that build up the HTTP request pipeline.
/// </summary>
public static class RequestPipelineExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline: base path, health checks, exception handling,
    /// correlation IDs, API documentation, static files and FastEndpoints.
    /// </summary>
    /// <param name="app">The web application whose request pipeline is configured.</param>
    /// <returns>The same <paramref name="app"/> instance so calls can be chained.</returns>
    public static WebApplication ConfigureRequestPipeline(this WebApplication app)
    {
        // Serve the application under the configured service base path.
        app.UsePathBase(ApiConstants.BasePath);

        app.MapHealthCheckEndpoints();

        // Handle unhandled exceptions and convert them to Problem Details (RFC 7807)
        app.UseExceptionHandler();

        // Capture or generate correlation ID for request tracking
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Convert HTTP status codes to Problem Details responses
        app.UseStatusCodePages();

        app.MapApiDocumentation();

        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Map FastEndpoints.
        app.UseFastEndpoints();

        return app;
    }

    /// <summary>
    /// Maps the liveness and readiness health check endpoints.
    /// </summary>
    /// <param name="app">The web application to map the health check endpoints on.</param>
    /// <returns>The same <paramref name="app"/> instance so calls can be chained.</returns>
    private static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(ApiConstants.Health.LiveRoute, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteHealthResponse,
        });

        app.MapHealthChecks(ApiConstants.Health.ReadyRoute, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckConstants.ReadyTag),
            ResponseWriter = WriteHealthResponse,
        });

        return app;
    }

    /// <summary>
    /// Writes a minimal JSON health response: the overall status plus each check's name and status.
    /// Deliberately omits exception and description detail so these unauthenticated endpoints don't
    /// leak internal information (such as database connection errors).
    /// </summary>
    /// <param name="context">The HTTP context to write the response to.</param>
    /// <param name="report">The aggregated health report to serialize.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    private static Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = HttpConstants.ApplicationJson;

        var payload = new
        {
            Status = report.Status.ToString(),
            Checks = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
            }),
        };

        return context.Response.WriteAsJsonAsync(payload);
    }

    /// <summary>
    /// Maps the OpenAPI document and Scalar API reference UI when
    /// <c>Features:ApiDocumentationEnabled</c> is enabled.
    /// </summary>
    /// <param name="app">The web application to map the API documentation on.</param>
    /// <returns>The same <paramref name="app"/> instance so calls can be chained.</returns>
    private static WebApplication MapApiDocumentation(this WebApplication app)
    {
        // Configure the HTTP request pipeline
        // API documentation is enabled via Features:ApiDocumentationEnabled configuration
        var apiDocumentationEnabled = app.Configuration.GetValue(ConfigurationConstants.ApiDocumentationEnabled, false);
        if (!apiDocumentationEnabled)
        {
            return app;
        }

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            // Group endpoints by OpenAPI tags and keep operations ordered by path within each resource.
            options.TagSorter = TagSorter.Alpha;
            options.EnabledClients = [ScalarClient.HttpClient, ScalarClient.Axios, ScalarClient.Fetch];
            options.EnabledTargets = [ScalarTarget.CSharp, ScalarTarget.JavaScript];
            options.Theme = ScalarTheme.Solarized;
            options.Favicon = ApiConstants.FaviconPath;
            options.EndpointPathPrefix = ApiConstants.ApiDocsRoute;
            options.Title = ScalarConstants.ApiDocumentationTitle;
        });

        return app;
    }
}
