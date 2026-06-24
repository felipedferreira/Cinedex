using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Movies.WebService.Constants;

namespace Movies.WebService.ExceptionHandlers;

internal sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.ContentType = HttpConstants.ProblemJson;
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var errors = validationException.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        var response = new
        {
            type = ProblemDetailsConstants.BadRequestType,
            title = ProblemDetailsConstants.BadRequestTitle,
            status = StatusCodes.Status400BadRequest,
            detail = ProblemDetailsConstants.ValidationErrorDetail,
            instance = httpContext.Request.Path.Value,
            errors,
            extensions = new { traceId },
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response), cancellationToken);
        return true;
    }
}
