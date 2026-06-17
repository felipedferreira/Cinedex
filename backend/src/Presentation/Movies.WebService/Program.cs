using FastEndpoints;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Movies.Application;
using Movies.Persistence.Postgres;
using Movies.WebService.ExceptionHandlers;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

namespace Movies.WebService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure OpenTelemetry logs + traces, exported to Seq over OTLP.
        // The OTLP endpoint, protocol and the Seq API-key header are supplied via the
        // standard OTEL_EXPORTER_OTLP_* environment variables (see compose.yaml). When no
        // endpoint is configured (local `dotnet run`, integration tests) the exporters are
        // omitted so nothing tries to reach a Seq that isn't there.
        var otlpEndpointConfigured =
            !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "Movies.WebService";

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Npgsql"); // EF Core/Npgsql emit DB spans on this ActivitySource

                if (otlpEndpointConfigured)
                {
                    tracing.AddOtlpExporter();
                }
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
            logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));

            if (otlpEndpointConfigured)
            {
                logging.AddOtlpExporter();
            }
        });

        // Configure Kestrel server timeouts
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var timeoutSeconds = context.Configuration.GetValue("WebServer:RequestTimeoutSeconds", 30);

            // RequestHeadersTimeout: Maximum time to receive complete HTTP request headers per request
            // Protects against slowloris attacks where clients slowly send header data
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(timeoutSeconds);

            // KeepAliveTimeout: Maximum time server waits for next request on idle connection
            // Prevents idle connections from consuming server resources indefinitely
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(1);
        });

        builder.Services
            .AddApplication()
            .AddPersistence();

        // Register FastEndpoints (discovers endpoint classes in this assembly)
        builder.Services.AddFastEndpoints();

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();

        builder.Services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
                name: "postgres",
                tags: ["ready"]);

        // Register exception handlers in chain order — DefaultExceptionHandler must be last (catch-all)
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<EntityNotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<DefaultExceptionHandler>();

        var app = builder.Build();

        // Serve the application under the "/movies-svc" base path
        app.UsePathBase("/movies-svc");

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        // Handle unhandled exceptions and convert them to Problem Details (RFC 7807)
        app.UseExceptionHandler();

        // Capture or generate correlation ID for request tracking
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Convert HTTP status codes to Problem Details responses
        app.UseStatusCodePages();

        // Configure the HTTP request pipeline
        // API documentation is enabled via Features:ApiDocumentationEnabled configuration
        var apiDocumentationEnabled = app.Configuration.GetValue("Features:ApiDocumentationEnabled", false);
        if (apiDocumentationEnabled)
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                // Group endpoints by HTTP method (Scalar's supported operation sorter)
                options.OperationSorter = OperationSorter.Method;
                options.EnabledClients = [ScalarClient.HttpClient, ScalarClient.Axios, ScalarClient.Fetch];
                options.EnabledTargets = [ScalarTarget.CSharp, ScalarTarget.JavaScript];
                options.Theme = ScalarTheme.Solarized;
                options.Favicon = "/favicon.ico";
                options.EndpointPathPrefix = "/api-docs/{documentName}";
                options.Title = "API Documentation - {documentName}";
            });
        }

        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Map FastEndpoints (Movies, ...)
        app.UseFastEndpoints();

        await app.RunAsync();
    }
}