using System.Diagnostics;

namespace Movies.WebService;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Attach the correlation id to every log record emitted during the request
        // (OpenTelemetry logging is configured with IncludeScopes = true) and stamp it on
        // the active trace so logs and the request's trace are searchable by it in Seq.
        var scopeState = new Dictionary<string, object> { ["CorrelationId"] = correlationId };
        Activity.Current?.SetTag("correlation_id", correlationId);

        using (logger.BeginScope(scopeState))
        {
            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);

            logger.LogInformation("Request started with CorrelationId: {CorrelationId}", correlationId);

            await next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader))
        {
            return correlationIdHeader.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}