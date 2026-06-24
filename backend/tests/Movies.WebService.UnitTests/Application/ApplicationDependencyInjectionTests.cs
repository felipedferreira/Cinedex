using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Genres.CreateGenre;
using Movies.Application.Genres.DeleteGenre;
using Movies.Application.Genres.GetGenreById;
using Movies.Application.Genres.ListGenres;
using Movies.Application.Genres.UpdateGenre;
using Movies.Application.Titles.CreateTitle;
using Movies.Application.Titles.DeleteTitle;
using Movies.Application.Titles.GetTitleById;
using Movies.Application.Titles.ListTitles;
using Movies.Application.Titles.UpdateTitle;
using Movies.WebService.UnitTests.TestDoubles;

namespace Movies.WebService.UnitTests.Application;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersHandlersAndValidators()
    {
        using ServiceProvider provider = TestServiceProvider.Create();
        using IServiceScope scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICreateTitleHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUpdateTitleHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDeleteTitleHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IGetTitleByIdHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IListTitlesHandler>());

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICreateGenreHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUpdateGenreHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDeleteGenreHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IGetGenreByIdHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IListGenresHandler>());

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IValidator<CreateTitleCommand>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IValidator<UpdateTitleCommand>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IValidator<CreateGenreCommand>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IValidator<UpdateGenreCommand>>());
    }
}
