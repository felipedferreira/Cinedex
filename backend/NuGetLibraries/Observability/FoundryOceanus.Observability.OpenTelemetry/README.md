# FoundryOceanus.Observability.OpenTelemetry

Shared OpenTelemetry setup for .NET hosts: **traces, metrics and logs**, exported over OTLP to any
OTLP backend — Seq, SigNoz, the .NET Aspire dashboard, an OpenTelemetry Collector. The transport is
**HTTP/protobuf by default, or gRPC**, chosen with the standard `OTEL_EXPORTER_OTLP_PROTOCOL` key.

`AddObservability` hangs off `IHostApplicationBuilder`, so the same call works for console hosts
(`Host.CreateApplicationBuilder`) and web hosts (`WebApplication.CreateBuilder`). Each host passes
its own instrumentation through `configureTracing` and `configureMetrics`; everything else — where
to send it, over which transport, which signals — comes from the standard `OTEL_*` configuration
keys, so no code names a backend.

## Usage

### Generic host (worker, migrator, console app)

```csharp
using FoundryOceanus.Observability.OpenTelemetry.Extensions;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddObservability(
    defaultServiceName: "MyWorker",
    configureTracing: tracing => tracing.AddSource("Npgsql"),
    configureMetrics: metrics => metrics.AddMeter("Npgsql"));
```

### Web host (ASP.NET Core)

```csharp
using FoundryOceanus.Observability.OpenTelemetry.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability(
    defaultServiceName: "MyWebService",
    configureTracing: tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Npgsql"),
    configureMetrics: metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("Npgsql"));
```

Instrumentation packages (`OpenTelemetry.Instrumentation.AspNetCore`, `...Http`) are referenced by
the consuming project, not by this library — it only carries the OTLP exporter and the hosting
integration.

### What every host gets

- **One resource for all three signals.** The service name resolves as `OTEL_SERVICE_NAME`, then the
  `defaultServiceName` argument, then the host's application name.
- **Traces** from whatever `configureTracing` registers.
- **Metrics** from the .NET runtime meter (`System.Runtime` — `dotnet.gc.*`, `dotnet.jit.*`,
  `dotnet.thread_pool.*`, `dotnet.exceptions` and so on), which is always added, plus whatever
  `configureMetrics` registers. On .NET 9 and later the runtime publishes that meter natively, so
  `OpenTelemetry.Instrumentation.Runtime` is not needed.
- **Logs** through the OpenTelemetry `ILogger` provider, with scopes and the formatted message
  included, correlated with the active trace. The provider alias is `OpenTelemetry`, so
  `Logging:OpenTelemetry:LogLevel:*` filters apply as usual.

## Configuration

Every key is read through the host's merged `IConfiguration`, so it can be an environment variable,
a **flat top-level key** in `appsettings.json` (e.g. `"OTEL_EXPORTER_OTLP_PROTOCOL": "grpc"`), a
user secret or a command-line argument.

| Key | Meaning | Default |
|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Base URL of the receiver, shared by all three signals. **This is the export switch**: unset or blank means no exporter is registered. When set it must be an absolute `http://` or `https://` URL. Give the base URL only — the exporter appends the signal path itself. | unset (no export) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `http/protobuf` or `grpc` (trimmed, case-insensitive). Anything else, `http/json` included, fails startup — but only when an endpoint is set. | **`http/protobuf`** |
| `OTEL_EXPORTER_OTLP_HEADERS`, `_TIMEOUT`, `_COMPRESSION`, `_CERTIFICATE`, `_CLIENT_CERTIFICATE`, `_CLIENT_KEY` | Read by the OpenTelemetry SDK, untouched by this library. Headers go out as HTTP headers or as gRPC metadata, depending on the transport. | SDK defaults |
| `OTEL_TRACES_EXPORTER`, `OTEL_METRICS_EXPORTER`, `OTEL_LOGS_EXPORTER` | `none` skips that one signal's exporter; its provider and instrumentation stay registered. Any other value means OTLP. | OTLP |
| `OTEL_SERVICE_NAME` | Overrides `defaultServiceName`. | `defaultServiceName` |
| `OTEL_RESOURCE_ATTRIBUTES`, `OTEL_BSP_*`, `OTEL_BLRP_*`, `OTEL_METRIC_EXPORT_INTERVAL`, `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE` | Passed through to the SDK unchanged. | SDK defaults (metrics every 60 s, cumulative) |
| `OTEL_SDK_DISABLED` | `true` makes the SDK build no-op providers: nothing is recorded or exported. Enforced by the SDK, which reads it from the final configuration; this library only reports it. | `false` |
| `OTEL_EXPORTER_OTLP_{TRACES,METRICS,LOGS}_{ENDPOINT,PROTOCOL,HEADERS,TIMEOUT,COMPRESSION}` | **Not supported** — the per-signal variants of the shared keys are not read. Setting one logs a startup warning. | ignored |

**Precedence.** Each key resolves on its own from the host's merged configuration, and the last
source that defines it wins; the library supplies `http/protobuf` only when
`OTEL_EXPORTER_OTLP_PROTOCOL` is absent from every source. For a default web host that order is
`appsettings.json` < `appsettings.{Environment}.json` < user secrets (Development only) <
environment variables < command line. Orchestrators (.NET Aspire, Docker Compose, most PaaS
consoles) set environment variables, so they win over anything committed in JSON.

**Call `AddObservability` after every configuration source has been added.** The export decision is
made once, when it runs. A host that adds its own JSON files must add them — and re-add environment
variables and the command line after them — before the call.

**Call it once.** A second call on the same builder throws `InvalidOperationException` rather than
registering a second exporter per signal.

## Choosing the transport

> **The default differs from the bare OpenTelemetry SDK.** The SDK defaults to gRPC; this library
> defaults to `http/protobuf`, so an endpoint set on its own targets an OTLP/HTTP receiver
> (conventionally port 4318). Because every key layers independently, **always set the endpoint
> and the protocol as a pair**: port 4317 with `grpc`, port 4318 with `http/protobuf`. Overriding
> only one of them from a higher-precedence source leaves the other from a lower one — a mismatch
> the startup warnings point out.

**Seq**, which accepts OTLP over gRPC only on HTTPS, so a plain-HTTP Seq needs `http/protobuf`:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://seq/ingest/otlp
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_HEADERS=X-Seq-ApiKey=your-api-key
```

The exporter appends `/v1/traces`, `/v1/metrics` and `/v1/logs` to the base path, so this posts to
`/ingest/otlp/v1/logs` and friends. If your Seq version does not ingest OTLP metrics, add
`OTEL_METRICS_EXPORTER=none`. Keep the `X-Seq-ApiKey` header out of any configuration that targets
a different backend.

**SigNoz, or any collector, over gRPC:**

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

Plaintext gRPC (`http://`, HTTP/2 without TLS) works as-is on .NET 5 and later. The
`AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true)` line
found in older samples was only ever needed on .NET Core 3.x; don't add it.

**A collector over HTTP:**

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4318
# OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf   (optional: it is the default)
```

**.NET Aspire** injects both `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_PROTOCOL=grpc`
into every project resource, so a host run under an AppHost reports to the dashboard with no
configuration of its own.

### Committing a transport

If a service's usual backend speaks gRPC, it can commit the protocol and leave only the endpoint to
each environment:

```json
{
  "OTEL_EXPORTER_OTLP_PROTOCOL": "grpc"
}
```

Setting `OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4317` is then enough. The flip side is the
same pairing rule: any environment that points the host at an HTTP-only receiver (Seq over plain
HTTP, a collector on 4318) must now set `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf` explicitly,
because the committed `grpc` would otherwise apply there too.

## Diagnostics

**Startup line.** Every host logs one `Information` line at startup stating exactly what it
exports, where and how — credentials in the endpoint are never printed:

```text
OpenTelemetry OTLP export: traces, metrics, logs over grpc to http://collector:4317/.
OpenTelemetry OTLP export: disabled (OTEL_EXPORTER_OTLP_ENDPOINT is not set).
OpenTelemetry OTLP export: disabled (OTEL_SDK_DISABLED=true).
```

**Warnings** follow it, as `OpenTelemetry OTLP configuration: …` at `Warning`, for configuration
that is valid but probably wrong:

- `grpc` against port 4318, or `http/protobuf` against port 4317;
- an endpoint that already ends in `/v1/traces`, `/v1/metrics` or `/v1/logs`;
- any `OTEL_EXPORTER_OTLP_{TRACES,METRICS,LOGS}_{ENDPOINT,PROTOCOL,HEADERS,TIMEOUT,COMPRESSION}` key.

**Fail-fast.** A configuration that cannot work throws `InvalidOperationException` from
`AddObservability`, with a message naming the key and the fix: an endpoint that is not an absolute
`http://`/`https://` URL (`collector:4317` parses as a URI whose scheme is `collector`), or a
protocol other than `grpc`/`http/protobuf`. The bare SDK would report either only on its internal
EventSource and quietly fall back to `localhost` or gRPC.

**SDK self-diagnostics.** When the line says export is on but nothing arrives, the SDK's own
EventSource usually knows why (connection refused, 404, TLS). Drop an `OTEL_DIAGNOSTICS.json` into
the process's working directory and the SDK writes its internal log there:

```json
{
  "LogDirectory": ".",
  "FileSize": 32768,
  "LogLevel": "Warning"
}
```

## Shutdown

Once every hosted service has stopped, and before the service provider is disposed, the library
force-flushes the logger provider and then the tracer provider within a shared 5-second budget.
This works around an OpenTelemetry 1.16 behaviour: over `http/protobuf` the log exporter creates
its `HttpClient` lazily, so a host that stops within one batch interval — a migrator, a one-shot
job — would otherwise make its first export during disposal and lose its final logs. Metrics need no
flush: the meter provider exports a final collection when it is disposed.

## Tests

A test host that loads real configuration (user secrets in Development, say) will export test
traffic to whatever backend that configuration names. Set `OTEL_SDK_DISABLED=true` in the test
host's configuration — an in-memory source is early enough, because the SDK reads it lazily from the
final configuration — and the startup line reports `disabled (OTEL_SDK_DISABLED=true)`.
