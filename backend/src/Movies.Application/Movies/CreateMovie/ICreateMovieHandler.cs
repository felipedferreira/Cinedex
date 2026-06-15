namespace Movies.Application.Movies.CreateMovie;

public interface ICreateMovieHandler
{
    Task<MovieDto> Handle(CreateMovieCommand command, CancellationToken cancellationToken);
}
