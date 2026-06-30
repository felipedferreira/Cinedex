using Cinedex.Application.Abstractions;
using Cinedex.Domain.GenreAggregate;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Genres.CreateGenre;

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