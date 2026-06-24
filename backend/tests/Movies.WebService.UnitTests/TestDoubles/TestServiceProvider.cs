using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Movies.Application;
using Movies.Application.Abstractions;

namespace Movies.WebService.UnitTests.TestDoubles;

internal static class TestServiceProvider
{
    public static ServiceProvider Create(
        ITitleRepository? titleRepository = null,
        IGenreRepository? genreRepository = null)
    {
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddSingleton<ITitleRepository>(titleRepository ?? new InMemoryTitleRepository());
        services.AddSingleton<IGenreRepository>(genreRepository ?? new InMemoryGenreRepository());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        return services.BuildServiceProvider(validateScopes: true);
    }
}
