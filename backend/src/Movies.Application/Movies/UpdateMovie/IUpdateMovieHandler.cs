namespace Movies.Application.Movies.UpdateMovie;

public interface IUpdateMovieHandler
{
    Task<MovieDto> Handle(UpdateMovieCommand command, CancellationToken cancellationToken);
}
