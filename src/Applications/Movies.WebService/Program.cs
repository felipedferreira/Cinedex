using FastEndpoints;
using Movies.Application;
using Movies.Persistance.Postgres;
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

        // Register application use cases and the PostgreSQL persistence adapter
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

        builder.Services
            .AddApplication()
            .AddPersistence(connectionString);

        // Register FastEndpoints (discovers endpoint classes in this assembly)
        builder.Services.AddFastEndpoints();

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        // Verify the database is reachable before the application starts serving requests.
        // MoviesDbContext is registered as a scoped service, so resolve it from a temporary
        // DI scope rather than the root provider, and call CanConnectAsync to test the connection.
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MoviesDbContext>();

            if (await dbContext.Database.CanConnectAsync())
            {
                app.Logger.LogInformation("Successfully connected to the database.");
            }
            else
            {
                app.Logger.LogError("Unable to connect to the database.");
            }
        }

        // Serve the application under the "/api" base path
        app.UsePathBase("/api");

        // Handle unhandled exceptions and convert them to Problem Details (RFC 7807)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

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

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
        };

        app.MapGet("/test-exception", () =>
            {
                throw new InvalidOperationException("This is a test exception to verify exception handling middleware.");
            })
            .WithName("TestException");

        app.MapGet("/weatherforecast", () =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        (
                            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            summaries[Random.Shared.Next(summaries.Length)]
                        ))
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast");

        await app.RunAsync();
    }
}