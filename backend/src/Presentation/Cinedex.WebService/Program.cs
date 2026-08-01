using Cinedex.Observability.OpenTelemetry.Extensions;
using Cinedex.WebService.Extensions;
using OpenTelemetry.Trace;

namespace Cinedex.WebService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddObservability(
            defaultServiceName: "Cinedex.WebService",
            configureTracing: tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("Npgsql")); // EF Core/Npgsql emit DB spans on this ActivitySource

        builder
            .ConfigureWebServer()
            .AddPresentationServices();

        var app = builder.Build();

        app.ConfigureRequestPipeline();

        await app.RunAsync();
    }
}
