using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Movies.WebService.Constants;

namespace Movies.WebService.ExceptionHandlers;

internal sealed class DefaultExceptionHandler(ILogger<DefaultExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, LogMessageConstants.UnhandledExceptionOccurred);

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.ContentType = HttpConstants.ProblemJson;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            type = ProblemDetailsConstants.InternalServerErrorType,
            title = ProblemDetailsConstants.InternalServerErrorTitle,
            status = StatusCodes.Status500InternalServerError,
            detail = ProblemDetailsConstants.InternalServerErrorDetail,
            instance = httpContext.Request.Path.Value,
            extensions = new { traceId },
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response), cancellationToken);
        return true;
    }
}
