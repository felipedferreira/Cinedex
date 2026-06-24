using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Movies.Application.Exceptions;
using Movies.WebService.Constants;

namespace Movies.WebService.ExceptionHandlers;

internal sealed class EntityNotFoundExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not EntityNotFoundException entityNotFoundException)
        {
            return false;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.ContentType = HttpConstants.ProblemJson;
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        var response = new
        {
            type = ProblemDetailsConstants.NotFoundType,
            title = ProblemDetailsConstants.NotFoundTitle,
            status = StatusCodes.Status404NotFound,
            detail = entityNotFoundException.Message,
            instance = httpContext.Request.Path.Value,
            extensions = new { traceId },
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response), cancellationToken);
        return true;
    }
}
