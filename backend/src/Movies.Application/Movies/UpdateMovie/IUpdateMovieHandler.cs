namespace Movies.Application.Movies.UpdateMovie;

public interface IUpdateMovieHandler
{
    Task<MovieDetailsDto> Handle(UpdateMovieCommand command, CancellationToken cancellationToken);
}
