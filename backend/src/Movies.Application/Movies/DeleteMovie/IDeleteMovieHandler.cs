namespace Movies.Application.Movies.DeleteMovie;

public interface IDeleteMovieHandler
{
    Task Handle(DeleteMovieCommand command, CancellationToken cancellationToken);
}