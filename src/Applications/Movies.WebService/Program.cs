using Movies.WebService.Middleware;

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

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        // Handle unhandled exceptions and convert them to Problem Details (RFC 7807)
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Capture or generate correlation ID for request tracking
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Convert HTTP status codes to Problem Details responses
        app.UseStatusCodePages();

        // Wire up Serilog request logging middleware
        app.UseSerilogRequestLogging();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
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