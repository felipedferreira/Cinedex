namespace Movies.Application.Movies.DeleteMovie;

public interface IDeleteMovieHandler
{
    Task<bool> Handle(DeleteMovieCommand command, CancellationToken cancellationToken);
}
