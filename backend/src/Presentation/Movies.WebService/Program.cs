using Movies.WebService.Extensions;

namespace Movies.WebService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder
            .AddObservability()
            .ConfigureWebServer()
            .AddPresentationServices();

        var app = builder.Build();

        app.ConfigureRequestPipeline();

        await app.RunAsync();
    }
}