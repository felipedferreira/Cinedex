using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Movies.CreateMovie;
using Movies.Application.Movies.DeleteMovie;
using Movies.Application.Movies.GetMovieById;
using Movies.Application.Movies.ListMovies;
using Movies.Application.Movies.UpdateMovie;

namespace Movies.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateMovieHandler, CreateMovieHandler>();
        services.AddScoped<IUpdateMovieHandler, UpdateMovieHandler>();
        services.AddScoped<IDeleteMovieHandler, DeleteMovieHandler>();
        services.AddScoped<IGetMovieByIdHandler, GetMovieByIdHandler>();
        services.AddScoped<IListMoviesHandler, ListMoviesHandler>();

        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            ServiceLifetime.Scoped,
            includeInternalTypes: true);

        return services;
    }
}
