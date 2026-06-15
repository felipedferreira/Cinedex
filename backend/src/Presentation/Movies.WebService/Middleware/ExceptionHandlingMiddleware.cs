using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using Movies.Application.Exceptions;

namespace Movies.WebService;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (EntityNotFoundException ex)
        {
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, logger);
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        var response = new
        {
            type = "https://httpwg.org/specs/rfc7231.html#status.400",
            title = "Bad Request",
            status = StatusCodes.Status400BadRequest,
            detail = "One or more validation errors occurred.",
            instance = context.Request.Path.Value,
            errors,
            extensions = new { traceId },
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    private static async Task HandleNotFoundExceptionAsync(HttpContext context, EntityNotFoundException exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        var response = new
        {
            type = "https://httpwg.org/specs/rfc7231.html#status.404",
            title = "Not Found",
            status = StatusCodes.Status404NotFound,
            detail = exception.Message,
            instance = context.Request.Path.Value,
            extensions = new { traceId },
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
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
            extensions = new { traceId },
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}