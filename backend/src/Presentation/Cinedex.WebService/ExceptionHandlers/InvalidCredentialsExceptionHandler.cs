using System.Diagnostics;
using System.Net.Mime;
using System.Text.Json;
using Cinedex.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Cinedex.WebService.ExceptionHandlers;

internal sealed class InvalidCredentialsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not InvalidCredentialsException invalidCredentialsException)
        {
            return false;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        var response = new
        {
            type = "https://httpwg.org/specs/rfc7235.html#status.401",
            title = "Unauthorized",
            status = StatusCodes.Status401Unauthorized,
            detail = invalidCredentialsException.Message,
            instance = httpContext.Request.Path.Value,
            extensions = new { traceId },
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response), cancellationToken);
        return true;
    }
}
