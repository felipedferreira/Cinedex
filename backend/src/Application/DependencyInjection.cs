using Cinedex.Application.Genres.CreateGenre;
using Cinedex.Application.Genres.DeleteGenre;
using Cinedex.Application.Genres.GetGenreById;
using Cinedex.Application.Genres.ListGenres;
using Cinedex.Application.Genres.UpdateGenre;
using Cinedex.Application.Titles.CreateTitle;
using Cinedex.Application.Titles.DeleteTitle;
using Cinedex.Application.Titles.GetTitleById;
using Cinedex.Application.Titles.ListTitles;
using Cinedex.Application.Titles.UpdateTitle;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cinedex.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // IoC for the title resource
        services.AddScoped<ICreateTitleHandler, CreateTitleHandler>();
        services.AddScoped<IUpdateTitleHandler, UpdateTitleHandler>();
        services.AddScoped<IDeleteTitleHandler, DeleteTitleHandler>();
        services.AddScoped<IGetTitleByIdHandler, GetTitleByIdHandler>();
        services.AddScoped<IListTitlesHandler, ListTitlesHandler>();

        // IoC for the genre resource
        services.AddScoped<ICreateGenreHandler, CreateGenreHandler>();
        services.AddScoped<IUpdateGenreHandler, UpdateGenreHandler>();
        services.AddScoped<IDeleteGenreHandler, DeleteGenreHandler>();
        services.AddScoped<IGetGenreByIdHandler, GetGenreByIdHandler>();
        services.AddScoped<IListGenresHandler, ListGenresHandler>();

        // IoC for FluentValidation
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            includeInternalTypes: true);

        return services;
    }
}