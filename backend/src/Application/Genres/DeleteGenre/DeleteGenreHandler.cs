using Cinedex.Application.Abstractions;
using Cinedex.Application.Exceptions;
using Cinedex.Domain.GenreAggregate;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Genres.DeleteGenre;

internal sealed class DeleteGenreHandler(
    IGenreRepository repository,
    ILogger<DeleteGenreHandler> logger) : IDeleteGenreHandler
{
    public async Task HandleAsync(DeleteGenreCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting genre {GenreId}.", command.Id);

        var deleted = await repository.DeleteAsync(command.Id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Genre {GenreId} was not found for deletion.", command.Id);
            throw new EntityNotFoundException(nameof(Genre), command.Id);
        }

        logger.LogInformation("Deleted genre {GenreId}.", command.Id);
    }
}