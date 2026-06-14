using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Abstractions;
using Movies.Persistence.Postgres.Repositories;

namespace Movies.Persistence.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MoviesDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IMovieRepository, MovieRepository>();

        return services;
    }
}
