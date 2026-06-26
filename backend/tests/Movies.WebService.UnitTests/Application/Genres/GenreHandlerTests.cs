using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Exceptions;
using Movies.Application.Genres.CreateGenre;
using Movies.Application.Genres.DeleteGenre;
using Movies.Application.Genres.UpdateGenre;
using Movies.WebService.UnitTests.TestDoubles;

namespace Movies.WebService.UnitTests.Application.Genres;

public sealed class GenreHandlerTests
{
    [Fact]
    public async Task CreateGenre_WithValidCommand_CreatesGenre()
    {
        var repository = new InMemoryGenreRepository();

        using ServiceProvider provider = TestServiceProvider.Create(genreRepository: repository);
        using IServiceScope scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICreateGenreHandler>();

        var result = await handler.HandleAsync(new CreateGenreCommand("Noir"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(1, repository.CreateCallCount);
        Assert.NotNull(repository.LastCreated);
        Assert.Equal(result, repository.LastCreated.Id);
        Assert.Equal("Noir", repository.LastCreated.Name);
    }

    [Fact]
    public async Task UpdateGenre_WithEmptyId_ThrowsValidationException()
    {
        var repository = new InMemoryGenreRepository();

        using ServiceProvider provider = TestServiceProvider.Create(genreRepository: repository);
        using IServiceScope scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IUpdateGenreHandler>();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.HandleAsync(new UpdateGenreCommand(Guid.Empty, "Noir"), CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(UpdateGenreCommand.Id));
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task DeleteGenre_WhenRepositoryMisses_ThrowsEntityNotFoundException()
    {
        var repository = new InMemoryGenreRepository
        {
            DeleteResult = false,
        };

        using ServiceProvider provider = TestServiceProvider.Create(genreRepository: repository);
        using IServiceScope scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IDeleteGenreHandler>();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.HandleAsync(new DeleteGenreCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(1, repository.DeleteCallCount);
    }
}
