using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Exceptions;
using Movies.Domain.Movies;

namespace Movies.Application.Movies.DeleteMovie;

internal sealed class DeleteMovieHandler(
    IMovieRepository repository,
    ILogger<DeleteMovieHandler> logger) : IDeleteMovieHandler
{
    public async Task Handle(DeleteMovieCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting movie {MovieId}.", command.Id);

        var deleted = await repository.DeleteAsync(command.Id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Movie {MovieId} was not found for deletion.", command.Id);
            throw new EntityNotFoundException(nameof(Movie), command.Id);
        }

        logger.LogInformation("Deleted movie {MovieId}.", command.Id);
    }
}
