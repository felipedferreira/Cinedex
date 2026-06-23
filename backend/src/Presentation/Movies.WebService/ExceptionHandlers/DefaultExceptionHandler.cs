using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace Movies.WebService.ExceptionHandlers;

internal sealed class DefaultExceptionHandler(ILogger<DefaultExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred.");

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            type = "https://httpwg.org/specs/rfc7231.html#status.500",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = "An unhandled exception occurred.",
            instance = httpContext.Request.Path.Value,
            extensions = new { traceId },
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response), cancellationToken);
        return true;
    }
}