using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Exceptions;
using Movies.Application.Titles.CreateTitle;
using Movies.Application.Titles.UpdateTitle;
using Movies.Domain.Enums;
using Movies.Domain.GenreAggregate;
using Movies.WebService.UnitTests.TestDoubles;

namespace Movies.WebService.UnitTests.Application.Titles;

public sealed class TitleHandlerTests
{
    [Fact]
    public async Task CreateTitle_WithDuplicateGenreIds_DeduplicatesStoredLinks()
    {
        var action = new Genre { Id = Guid.NewGuid(), Name = "Action" };
        var drama = new Genre { Id = Guid.NewGuid(), Name = "Drama" };
        var titleRepository = new InMemoryTitleRepository();
        var genreRepository = new InMemoryGenreRepository(action, drama);

        using ServiceProvider provider = TestServiceProvider.Create(titleRepository, genreRepository);
        using IServiceScope scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICreateTitleHandler>();

        var result = await handler.HandleAsync(
            new CreateTitleCommand(
                "Heat",
                TitleType.Movie,
                1995,
                "A crime drama.",
                [action.Id, drama.Id, action.Id]),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(titleRepository.LastCreated);
        Assert.Equal(result, titleRepository.LastCreated.Id);
        Assert.Equal("Heat", titleRepository.LastCreated.Name);
        Assert.Equal([action.Id, drama.Id], titleRepository.LastCreated.GenreIds);
    }

    [Fact]
    public async Task CreateTitle_WhenAnyGenreIsMissing_ThrowsEntityNotFoundException()
    {
        var knownGenre = new Genre { Id = Guid.NewGuid(), Name = "Action" };
        var missingGenreId = Guid.NewGuid();
        var titleRepository = new InMemoryTitleRepository();
        var genreRepository = new InMemoryGenreRepository(knownGenre);

        using ServiceProvider provider = TestServiceProvider.Create(titleRepository, genreRepository);
        using IServiceScope scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICreateTitleHandler>();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.HandleAsync(
                new CreateTitleCommand(
                    "Heat",
                    TitleType.Movie,
                    1995,
                    null,
                    [knownGenre.Id, missingGenreId]),
                CancellationToken.None));

        Assert.Equal(0, titleRepository.CreateCallCount);
    }

    [Fact]
    public async Task CreateTitle_WithInvalidCommand_ThrowsValidationExceptionBeforeRepositoryCalls()
    {
        var titleRepository = new InMemoryTitleRepository();
        var genreRepository = new InMemoryGenreRepository();

        using ServiceProvider provider = TestServiceProvider.Create(titleRepository, genreRepository);
        using IServiceScope scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICreateTitleHandler>();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(
                new CreateTitleCommand(
                    string.Empty,
                    (TitleType)999,
                    1800,
                    new string('x', 2001),
                    [Guid.Empty]),
                CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateTitleCommand.Title));
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateTitleCommand.Type));
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateTitleCommand.YearOfRelease));
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateTitleCommand.Description));
        Assert.Contains(exception.Errors, error => error.PropertyName == $"{nameof(CreateTitleCommand.GenreIds)}[0]");
        Assert.Equal(0, titleRepository.CreateCallCount);
        Assert.Empty(genreRepository.LastRequestedIds);
    }

    [Fact]
    public async Task UpdateTitle_WhenRepositoryMisses_ThrowsEntityNotFoundException()
    {
        var genre = new Genre { Id = Guid.NewGuid(), Name = "Drama" };
        var titleRepository = new InMemoryTitleRepository
        {
            UpdateResult = false,
        };
        var genreRepository = new InMemoryGenreRepository(genre);

        using ServiceProvider provider = TestServiceProvider.Create(titleRepository, genreRepository);
        using IServiceScope scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IUpdateTitleHandler>();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.HandleAsync(
                new UpdateTitleCommand(
                    Guid.NewGuid(),
                    "Updated",
                    TitleType.Movie,
                    2025,
                    null,
                    [genre.Id]),
                CancellationToken.None));

        Assert.Equal(1, titleRepository.UpdateCallCount);
    }
}
