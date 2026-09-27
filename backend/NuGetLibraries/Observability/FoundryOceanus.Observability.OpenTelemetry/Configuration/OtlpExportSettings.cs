using FoundryOceanus.Observability.OpenTelemetry.Constants;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Exporter;

namespace FoundryOceanus.Observability.OpenTelemetry.Configuration;

/// <summary>
/// What <c>AddObservability</c> exports and over which OTLP transport, resolved once from the merged
/// configuration when it is called.
/// </summary>
internal sealed class OtlpExportSettings
{
    /// <summary>
    /// The transport used when OTEL_EXPORTER_OTLP_PROTOCOL is unset. Deliberately not the SDK's own default,
    /// which is gRPC on .NET 5 and later.
    /// </summary>
    internal const OtlpExportProtocol DefaultProtocol = OtlpExportProtocol.HttpProtobuf;

    private const string GrpcValue = "grpc";
    private const string HttpProtobufValue = "http/protobuf";
    private const string NoneValue = "none";
    private const int ConventionalGrpcPort = 4317;
    private const int ConventionalHttpPort = 4318;

    private OtlpExportSettings(
        Uri? endpoint,
        OtlpExportProtocol protocol,
        bool exportTraces,
        bool exportMetrics,
        bool exportLogs,
        IReadOnlyList<string> warnings)
    {
        Endpoint = endpoint;
        Protocol = protocol;
        ExportTraces = exportTraces;
        ExportMetrics = exportMetrics;
        ExportLogs = exportLogs;
        Warnings = warnings;
    }

    /// <summary>
    /// Gets the configured base endpoint, or <see langword="null"/> when no endpoint is configured.
    /// </summary>
    internal Uri? Endpoint { get; }

    /// <summary>
    /// Gets the transport every exporter uses.
    /// </summary>
    internal OtlpExportProtocol Protocol { get; }

    /// <summary>
    /// Gets a value indicating whether an OTLP trace exporter is registered.
    /// </summary>
    internal bool ExportTraces { get; }

    /// <summary>
    /// Gets a value indicating whether an OTLP metric exporter is registered.
    /// </summary>
    internal bool ExportMetrics { get; }

    /// <summary>
    /// Gets a value indicating whether an OTLP log exporter is registered.
    /// </summary>
    internal bool ExportLogs { get; }

    /// <summary>
    /// Gets a value indicating whether at least one signal is exported.
    /// </summary>
    internal bool IsEnabled => ExportTraces || ExportMetrics || ExportLogs;

    /// <summary>
    /// Gets configuration that is valid but probably wrong, reported once at startup.
    /// </summary>
    internal IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// Resolves the export settings from the host's merged configuration.
    /// </summary>
    /// <param name="configuration">The host configuration, with every source already added.</param>
    /// <returns>The resolved settings.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown only when an endpoint is configured and either the endpoint is not an absolute http:// or
    /// https:// URL or the protocol is neither "grpc" nor "http/protobuf".
    /// </exception>
    internal static OtlpExportSettings Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? rawEndpoint = configuration[ConfigurationConstants.OtlpEndpoint];

        if (string.IsNullOrWhiteSpace(rawEndpoint))
        {
            // Nothing is exported, so nothing else is validated: a stray value cannot stop a host that is
            // not exporting.
            return new OtlpExportSettings(
                endpoint: null,
                DefaultProtocol,
                exportTraces: false,
                exportMetrics: false,
                exportLogs: false,
                warnings: []);
        }

        Uri endpoint = ParseEndpoint(rawEndpoint);
        OtlpExportProtocol protocol = ParseProtocol(configuration[ConfigurationConstants.OtlpProtocol]);

        return new OtlpExportSettings(
            endpoint,
            protocol,
            exportTraces: !IsNone(configuration[ConfigurationConstants.TracesExporter]),
            exportMetrics: !IsNone(configuration[ConfigurationConstants.MetricsExporter]),
            exportLogs: !IsNone(configuration[ConfigurationConstants.LogsExporter]),
            CollectWarnings(configuration, endpoint, protocol));
    }

    /// <summary>
    /// The configure delegate handed to every <c>AddOtlpExporter</c>. The SDK has already filled
    /// <paramref name="options"/> from the OTEL_EXPORTER_OTLP_* keys and runs this afterwards, so the value set
    /// here wins. It sets the protocol only and never the endpoint: assigning
    /// <see cref="OtlpExporterOptions.Endpoint"/> would stop the SDK appending /v1/{signal} for http/protobuf.
    /// </summary>
    /// <param name="options">The per-signal exporter options the SDK has just created.</param>
    internal void Apply(OtlpExporterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Protocol = Protocol;
    }

    /// <summary>
    /// Describes the effective export for the startup log line. Never includes credentials.
    /// </summary>
    /// <returns>For example "traces, metrics, logs over grpc to http://odin:4317/".</returns>
    internal string Describe()
    {
        if (Endpoint is null)
        {
            return $"disabled ({ConfigurationConstants.OtlpEndpoint} is not set)";
        }

        if (!IsEnabled)
        {
            return $"disabled ({ConfigurationConstants.TracesExporter}, {ConfigurationConstants.MetricsExporter} and {ConfigurationConstants.LogsExporter} are all '{NoneValue}')";
        }

        var signals = new List<string>(3);

        if (ExportTraces)
        {
            signals.Add("traces");
        }

        if (ExportMetrics)
        {
            signals.Add("metrics");
        }

        if (ExportLogs)
        {
            signals.Add("logs");
        }

        // Uri.Authority leaves out any user:password@ part, so credentials never reach the log.
        return $"{string.Join(", ", signals)} over {ProtocolName(Protocol)} to {Endpoint.Scheme}://{Endpoint.Authority}{Endpoint.AbsolutePath}";
    }

    private static Uri ParseEndpoint(string rawEndpoint)
    {
        // "odin:4317" parses as an absolute URI whose scheme is "odin", hence the explicit scheme check. The SDK
        // would only report a malformed value on its EventSource and fall back to localhost.
        if (Uri.TryCreate(rawEndpoint.Trim(), UriKind.Absolute, out Uri? endpoint)
            && (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps))
        {
            return endpoint;
        }

        throw new InvalidOperationException(
            $"{ConfigurationConstants.OtlpEndpoint} '{rawEndpoint}' is not an absolute http:// or https:// URL. " +
            $"Use a base URL such as 'http://collector:{ConventionalHttpPort}' (http/protobuf) or 'http://collector:{ConventionalGrpcPort}' (grpc).");
    }

    private static OtlpExportProtocol ParseProtocol(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return DefaultProtocol;
        }

        // Case-insensitive on purpose: the resolved value is assigned in code, so the SDK's own case-sensitive
        // parser (which silently falls back to gRPC) never decides the transport.
        if (string.Equals(trimmed, GrpcValue, StringComparison.OrdinalIgnoreCase))
        {
            return OtlpExportProtocol.Grpc;
        }

        if (string.Equals(trimmed, HttpProtobufValue, StringComparison.OrdinalIgnoreCase))
        {
            return OtlpExportProtocol.HttpProtobuf;
        }

        throw new InvalidOperationException(
            $"{ConfigurationConstants.OtlpProtocol} '{value}' is not supported. " +
            $"Use '{HttpProtobufValue}' (the default, conventionally port {ConventionalHttpPort}) or '{GrpcValue}' (conventionally port {ConventionalGrpcPort}).");
    }

    private static bool IsNone(string? value) =>
        string.Equals(value?.Trim(), NoneValue, StringComparison.OrdinalIgnoreCase);

    private static string ProtocolName(OtlpExportProtocol protocol) =>
        protocol == OtlpExportProtocol.Grpc ? GrpcValue : HttpProtobufValue;

    private static List<string> CollectWarnings(IConfiguration configuration, Uri endpoint, OtlpExportProtocol protocol)
    {
        var warnings = new List<string>();

        if (protocol == OtlpExportProtocol.Grpc && endpoint.Port == ConventionalHttpPort)
        {
            warnings.Add($"grpc is configured against port {ConventionalHttpPort}, the conventional OTLP/HTTP port. Set {ConfigurationConstants.OtlpEndpoint} and {ConfigurationConstants.OtlpProtocol} as a pair: port {ConventionalGrpcPort} with grpc, port {ConventionalHttpPort} with http/protobuf.");
        }
        else if (protocol == OtlpExportProtocol.HttpProtobuf && endpoint.Port == ConventionalGrpcPort)
        {
            warnings.Add($"http/protobuf is configured against port {ConventionalGrpcPort}, the conventional OTLP/gRPC port. Set {ConfigurationConstants.OtlpProtocol}=grpc, or use port {ConventionalHttpPort}.");
        }

        string path = endpoint.AbsolutePath.TrimEnd('/');

        if (path.EndsWith("/v1/traces", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/v1/metrics", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/v1/logs", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"{ConfigurationConstants.OtlpEndpoint} ends with a per-signal path. Give the base URL only; the exporter appends /v1/traces, /v1/metrics and /v1/logs itself.");
        }

        string[] signals = ["TRACES", "METRICS", "LOGS"];
        string[] settings = ["ENDPOINT", "PROTOCOL", "HEADERS", "TIMEOUT", "COMPRESSION"];

        foreach (string signal in signals)
        {
            foreach (string setting in settings)
            {
                string key = $"OTEL_EXPORTER_OTLP_{signal}_{setting}";

                if (!string.IsNullOrWhiteSpace(configuration[key]))
                {
                    warnings.Add($"{key} is ignored: the OTEL_EXPORTER_OTLP_* keys apply to all three signals and per-signal variants are not read.");
                }
            }
        }

        return warnings;
    }
}