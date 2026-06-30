using Cinedex.Application.Abstractions;
using Cinedex.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cinedex.Persistence.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceAdapter(this IServiceCollection services)
    {
        services.AddDbContext<FilmDbContext>((sp, options) =>
        {
            var configuation = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuation.GetConnectionString("DefaultConnection");
            options
                .UseNpgsql(connectionString)
                .UseCamelCaseNamingConvention();
        });
        services.AddScoped<ITitleRepository, TitleRepository>();
        services.AddScoped<IGenreRepository, GenreRepository>();

        return services;
    }
}