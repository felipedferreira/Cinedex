using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Primitives;

namespace FoundryOceanus.Observability.OpenTelemetry.Tests.Fakes;

/// <summary>
/// A minimal OTLP receiver on loopback. It listens twice on ephemeral ports: once for cleartext HTTP/2 only
/// (h2c with prior knowledge, which is what the gRPC exporter speaks on an http:// endpoint) and once for
/// HTTP/1.1 only (what the http/protobuf exporter speaks). Every POST is recorded and acknowledged with an
/// empty success response in the matching framing, so the exporters treat each export as delivered.
/// </summary>
internal sealed class FakeOtlpReceiver : IAsyncDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    private readonly ConcurrentQueue<ReceivedRequest> requests = new();
    private WebApplication? app;
    private ListenOptions? grpcListener;
    private ListenOptions? httpListener;

    private FakeOtlpReceiver()
    {
    }

    /// <summary>
    /// Gets the base URL of the HTTP/2-only listener, for the grpc transport.
    /// </summary>
    public Uri GrpcEndpoint => ToEndpoint(grpcListener);

    /// <summary>
    /// Gets the base URL of the HTTP/1.1-only listener, for the http/protobuf transport.
    /// </summary>
    public Uri HttpEndpoint => ToEndpoint(httpListener);

    /// <summary>
    /// Gets a snapshot of every request received so far, in arrival order.
    /// </summary>
    public IReadOnlyList<ReceivedRequest> Requests => [.. requests];

    /// <summary>
    /// Starts a receiver on two ephemeral loopback ports.
    /// </summary>
    /// <returns>The running receiver.</returns>
    public static async Task<FakeOtlpReceiver> StartAsync()
    {
        var receiver = new FakeOtlpReceiver();

        // The empty builder reads no configuration, environment variables or appsettings, so nothing on the
        // machine (ASPNETCORE_URLS, a Kestrel section) can move these listeners.
        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Loopback, 0, listen =>
            {
                listen.Protocols = HttpProtocols.Http2;
                receiver.grpcListener = listen;
            });
            kestrel.Listen(IPAddress.Loopback, 0, listen =>
            {
                listen.Protocols = HttpProtocols.Http1;
                receiver.httpListener = listen;
            });
        });

        receiver.app = builder.Build();
        receiver.app.Run(receiver.HandleAsync);
        await receiver.app.StartAsync();

        return receiver;
    }

    /// <summary>
    /// Waits until <paramref name="predicate"/> holds for the requests received so far.
    /// </summary>
    /// <param name="predicate">The condition over the received requests.</param>
    /// <param name="timeout">How long to wait before giving up.</param>
    /// <returns>The snapshot of requests that satisfied <paramref name="predicate"/>.</returns>
    /// <exception cref="TimeoutException">Thrown when the condition does not hold within the timeout.</exception>
    public async Task<IReadOnlyList<ReceivedRequest>> WaitForAsync(
        Func<IReadOnlyList<ReceivedRequest>, bool> predicate,
        TimeSpan timeout)
    {
        Stopwatch elapsed = Stopwatch.StartNew();

        while (true)
        {
            IReadOnlyList<ReceivedRequest> snapshot = Requests;

            if (predicate(snapshot))
            {
                return snapshot;
            }

            if (elapsed.Elapsed >= timeout)
            {
                string received = snapshot.Count == 0 ? "nothing" : string.Join("; ", snapshot);
                throw new TimeoutException($"The expected OTLP requests did not arrive within {timeout}. Received: {received}.");
            }

            await Task.Delay(PollInterval);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static Uri ToEndpoint(ListenOptions? listener)
    {
        // Kestrel writes the bound port back into the ListenOptions once it has bound port 0.
        int port = listener?.IPEndPoint?.Port ?? 0;

        if (port == 0)
        {
            throw new InvalidOperationException("The receiver has not bound its listeners yet.");
        }

        return new Uri($"http://127.0.0.1:{port}");
    }

    private async Task HandleAsync(HttpContext context)
    {
        HttpRequest request = context.Request;

        using var body = new MemoryStream();
        await request.Body.CopyToAsync(body, context.RequestAborted);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, StringValues> header in request.Headers)
        {
            headers[header.Key] = header.Value.ToString();
        }

        requests.Enqueue(new ReceivedRequest(
            request.Protocol,
            request.Path.Value ?? string.Empty,
            request.ContentType ?? string.Empty,
            headers,
            body.ToArray()));

        HttpResponse response = context.Response;
        response.StatusCode = StatusCodes.Status200OK;

        if (request.ContentType?.StartsWith(OtlpPaths.GrpcContentType, StringComparison.OrdinalIgnoreCase) == true)
        {
            // An empty Export*ServiceResponse: one uncompressed message frame (flag byte + 4-byte big-endian
            // length of zero), then the trailer that marks the call as successful.
            response.ContentType = OtlpPaths.GrpcContentType;
            await response.Body.WriteAsync(new byte[5], context.RequestAborted);
            response.AppendTrailer("grpc-status", "0");
            return;
        }

        response.ContentType = OtlpPaths.ProtobufContentType;
    }
}
