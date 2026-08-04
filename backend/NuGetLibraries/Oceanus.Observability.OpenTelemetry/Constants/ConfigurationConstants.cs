namespace Oceanus.Observability.OpenTelemetry.Constants;

/// <summary>
/// Standard OpenTelemetry environment variable keys for OTLP export configuration.
/// </summary>
public static class ConfigurationConstants
{
    /// <summary>
    /// OTEL_EXPORTER_OTLP_ENDPOINT — the OTLP ingestion endpoint (e.g., "http://seq/ingest/otlp").
    /// </summary>
    public const string OtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// OTEL_SERVICE_NAME — the logical service name that appears in traces and logs.
    /// </summary>
    public const string OtelServiceName = "OTEL_SERVICE_NAME";
}
