using Movies.Application.Abstractions;

namespace Movies.Application.Movies.DeleteMovie;

internal sealed class DeleteMovieHandler(IMovieRepository repository) : IDeleteMovieHandler
{
    public Task<bool> Handle(DeleteMovieCommand command, CancellationToken cancellationToken) =>
        repository.DeleteAsync(command.Id, cancellationToken);
}
