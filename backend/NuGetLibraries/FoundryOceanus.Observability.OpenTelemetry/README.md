# FoundryOceanus.Observability.OpenTelemetry

Shared OpenTelemetry setup for .NET services. Provides logging and tracing exported to OTLP collectors (e.g., Seq).

`AddObservability` hangs off `IHostApplicationBuilder`, so the same call works for console hosts
(`Host.CreateApplicationBuilder`) and web hosts (`WebApplication.CreateBuilder`). Each host passes its
own instrumentation through `configureTracing`.

## Usage

### Generic host (e.g. DatabaseMigrator, SchedulerWorker)

```csharp
using FoundryOceanus.Observability.OpenTelemetry.Extensions;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddObservability(
    defaultServiceName: "MyService",
    configureTracing: tracing => tracing.AddSource("Npgsql"));
```

### Web host (ASP.NET Core)

```csharp
using FoundryOceanus.Observability.OpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability(
    defaultServiceName: "MyWebService",
    configureTracing: tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Npgsql"));
```

Instrumentation packages (`OpenTelemetry.Instrumentation.AspNetCore`, `...Http`) are referenced by the
consuming project, not by this library — it only carries the exporter and hosting integration.

## Configuration

Set the standard OpenTelemetry environment variables to enable OTLP export:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://seq/ingest/otlp
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_HEADERS=X-Seq-ApiKey=your-api-key
OTEL_SERVICE_NAME=MyService
```

When `OTEL_EXPORTER_OTLP_ENDPOINT` is not set, logging and tracing are still configured but the
exporters are omitted — nothing tries to reach a Seq that isn't there (local `dotnet run`, tests).

The reported service name resolves in this order: `OTEL_SERVICE_NAME`, then the `defaultServiceName`
argument, then the host's application name.
