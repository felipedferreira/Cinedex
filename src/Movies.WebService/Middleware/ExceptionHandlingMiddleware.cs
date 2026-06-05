using System.Diagnostics;
using System.Text.Json;

namespace Movies.WebService.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, logger);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger<ExceptionHandlingMiddleware> logger)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            type = "https://httpwg.org/specs/rfc7231.html#status.500",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = "An unhandled exception occurred.",
            instance = context.Request.Path.Value,
            extensions = new { traceId, },
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}