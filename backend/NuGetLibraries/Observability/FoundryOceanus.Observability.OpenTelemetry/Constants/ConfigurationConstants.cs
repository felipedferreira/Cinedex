namespace FoundryOceanus.Observability.OpenTelemetry.Constants;

/// <summary>
/// Standard OpenTelemetry configuration keys read by this library. Each is read through the host's
/// <c>IConfiguration</c>, so it works as an environment variable, a top-level key in
/// appsettings.json/application.json, a user secret or a command-line argument, with the host's usual
/// precedence (the last source that defines a key wins). None of them is specific to this library.
/// </summary>
public static class ConfigurationConstants
{
    /// <summary>
    /// OTEL_EXPORTER_OTLP_ENDPOINT — the base URL of the OTLP receiver, shared by traces, metrics and logs
    /// (e.g., "http://collector:4318", "http://collector:4317" or "http://seq/ingest/otlp"). Unset or blank
    /// disables export; otherwise it must be an absolute http:// or https:// URL. Give the base URL only: the
    /// exporter appends /v1/traces, /v1/metrics and /v1/logs (http/protobuf) or the gRPC service path itself.
    /// </summary>
    public const string OtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// OTEL_SERVICE_NAME — the logical service name that appears on traces, metrics and logs.
    /// </summary>
    public const string OtelServiceName = "OTEL_SERVICE_NAME";

    /// <summary>
    /// OTEL_EXPORTER_OTLP_PROTOCOL — the OTLP transport: "http/protobuf" (this library's default when unset)
    /// or "grpc". Set it together with the endpoint: port 4318 is conventionally http/protobuf and 4317 grpc.
    /// </summary>
    public const string OtlpProtocol = "OTEL_EXPORTER_OTLP_PROTOCOL";

    /// <summary>
    /// OTEL_TRACES_EXPORTER — "none" turns trace export off; unset or any other value exports over OTLP.
    /// </summary>
    public const string TracesExporter = "OTEL_TRACES_EXPORTER";

    /// <summary>
    /// OTEL_METRICS_EXPORTER — "none" turns metric export off; unset or any other value exports over OTLP.
    /// </summary>
    public const string MetricsExporter = "OTEL_METRICS_EXPORTER";

    /// <summary>
    /// OTEL_LOGS_EXPORTER — "none" turns log export off; unset or any other value exports over OTLP.
    /// </summary>
    public const string LogsExporter = "OTEL_LOGS_EXPORTER";

    /// <summary>
    /// OTEL_SDK_DISABLED — "true" makes the OpenTelemetry SDK build no-op providers, so nothing is recorded
    /// or exported. The SDK enforces it; this library only reports it.
    /// </summary>
    public const string SdkDisabled = "OTEL_SDK_DISABLED";
}