using FastEndpoints;
using Movies.Application;
using Movies.Persistence.Postgres;
using Movies.WebService.ExceptionHandlers;
using Scalar.AspNetCore;
using Serilog;

namespace Movies.WebService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog
        builder.Host.UseSerilog((_, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .WriteTo.Console();
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

        // Register exception handlers in chain order — DefaultExceptionHandler must be last (catch-all)
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<EntityNotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<DefaultExceptionHandler>();

        var app = builder.Build();

        // Serve the application under the "/movies-svc" base path
        app.UsePathBase("/movies-svc");

        // Handle unhandled exceptions and convert them to Problem Details (RFC 7807)
        app.UseExceptionHandler();

        // Capture or generate correlation ID for request tracking
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Convert HTTP status codes to Problem Details responses
        app.UseStatusCodePages();

        // Wire up Serilog request logging middleware
        app.UseSerilogRequestLogging();

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