using Microsoft.Extensions.Hosting;

namespace Cinedex.SchedulerWorker;

public sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
            })
            .Build();

        try
        {
            await host.RunAsync();
            return 0;
        }
        catch (Exception)
        {
            return 1;
        }
    }
}
