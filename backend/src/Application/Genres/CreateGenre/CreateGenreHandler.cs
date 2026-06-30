using FluentValidation;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Domain.GenreAggregate;

namespace Movies.Application.Genres.CreateGenre;

internal sealed class CreateGenreHandler(
    IGenreRepository repository,
    IValidator<CreateGenreCommand> validator,
    ILogger<CreateGenreHandler> logger) : ICreateGenreHandler
{
    public async Task<Guid> HandleAsync(CreateGenreCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating genre {Name}.", command.Name);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var genre = Genre.Create(command.Name);

        await repository.CreateAsync(genre, cancellationToken);

        logger.LogInformation("Created genre {GenreId} ({Name}).", genre.Id, genre.Name);

        return genre.Id;
    }
}
