using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Constants;
using Movies.Application.Exceptions;
using Movies.Domain.GenreAggregate;

namespace Movies.Application.Genres.DeleteGenre;

internal sealed class DeleteGenreHandler(
    IGenreRepository repository,
    ILogger<DeleteGenreHandler> logger) : IDeleteGenreHandler
{
    public async Task Handle(DeleteGenreCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(LogMessageConstants.Genre.Deleting, command.Id);

        var deleted = await repository.DeleteAsync(command.Id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning(LogMessageConstants.Genre.NotFoundForDeletion, command.Id);
            throw new EntityNotFoundException(nameof(Genre), command.Id);
        }

        logger.LogInformation(LogMessageConstants.Genre.Deleted, command.Id);
    }
}
