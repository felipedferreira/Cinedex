using Cinedex.Application.Abstractions;
using Cinedex.Application.Exceptions;
using Cinedex.Domain.GenreAggregate;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Genres.UpdateGenre;

internal sealed class UpdateGenreHandler(
    IGenreRepository repository,
    IValidator<UpdateGenreCommand> validator,
    ILogger<UpdateGenreHandler> logger) : IUpdateGenreHandler
{
    public async Task HandleAsync(UpdateGenreCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating genre {GenreId}.", command.Id);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var genre = Genre.Create(command.Id, command.Name);

        var updated = await repository.UpdateAsync(genre, cancellationToken);

        if (!updated)
        {
            logger.LogWarning("Genre {GenreId} was not found for update.", command.Id);
            throw new EntityNotFoundException(nameof(Genre), command.Id);
        }

        logger.LogInformation("Updated genre {GenreId}.", genre.Id);
    }
}