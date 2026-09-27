namespace FoundryOceanus.Observability.OpenTelemetry.Tests.Fakes;

/// <summary>
/// The request paths and content types the OTLP exporters use, per transport.
/// </summary>
internal static class OtlpPaths
{
    /// <summary>The http/protobuf traces path, appended to the endpoint's base path.</summary>
    public const string HttpTraces = "/v1/traces";

    /// <summary>The http/protobuf metrics path, appended to the endpoint's base path.</summary>
    public const string HttpMetrics = "/v1/metrics";

    /// <summary>The http/protobuf logs path, appended to the endpoint's base path.</summary>
    public const string HttpLogs = "/v1/logs";

    /// <summary>The gRPC trace service method.</summary>
    public const string GrpcTraces = "/opentelemetry.proto.collector.trace.v1.TraceService/Export";

    /// <summary>The gRPC metrics service method.</summary>
    public const string GrpcMetrics = "/opentelemetry.proto.collector.metrics.v1.MetricsService/Export";

    /// <summary>The gRPC logs service method.</summary>
    public const string GrpcLogs = "/opentelemetry.proto.collector.logs.v1.LogsService/Export";

    /// <summary>The content type every http/protobuf export carries.</summary>
    public const string ProtobufContentType = "application/x-protobuf";

    /// <summary>The content type every gRPC export carries.</summary>
    public const string GrpcContentType = "application/grpc";
}
