using Movies.Application.Abstractions;
using Movies.Application.Exceptions;
using Movies.Domain;

namespace Movies.Application.Movies.DeleteMovie;

internal sealed class DeleteMovieHandler(IMovieRepository repository) : IDeleteMovieHandler
{
    public async Task Handle(DeleteMovieCommand command, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(command.Id, cancellationToken);

        if (!deleted)
        {
            throw new EntityNotFoundException(nameof(Movie), command.Id);
        }
    }
}
