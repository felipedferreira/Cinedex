using System.Text;

namespace FoundryOceanus.Observability.OpenTelemetry.Tests.Fakes;

/// <summary>
/// One POST the <see cref="FakeOtlpReceiver"/> accepted, exactly as it arrived on the wire.
/// </summary>
/// <param name="Protocol">The HTTP protocol Kestrel negotiated, e.g. "HTTP/1.1" or "HTTP/2".</param>
/// <param name="Path">The request path, including any base path the exporter kept.</param>
/// <param name="ContentType">The request content type, or an empty string when none was sent.</param>
/// <param name="Headers">The request headers, keyed case-insensitively.</param>
/// <param name="Body">The raw request body.</param>
internal sealed record ReceivedRequest(
    string Protocol,
    string Path,
    string ContentType,
    IReadOnlyDictionary<string, string> Headers,
    byte[] Body)
{
    /// <summary>
    /// Returns whether the body contains <paramref name="text"/> as UTF-8. OTLP protobuf stores strings as
    /// raw UTF-8, so a span name, service name or log line can be found without decoding the message.
    /// </summary>
    /// <param name="text">The text to look for.</param>
    /// <returns><see langword="true"/> when the UTF-8 bytes of <paramref name="text"/> appear in the body.</returns>
    public bool BodyContains(string text) =>
        Body.AsSpan().IndexOf(Encoding.UTF8.GetBytes(text)) >= 0;

    /// <inheritdoc/>
    public override string ToString() => $"{Protocol} POST {Path} ({ContentType}, {Body.Length} bytes)";
}
