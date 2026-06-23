namespace Movies.Application.Movies.CreateMovie;

public interface ICreateMovieHandler
{
    Task<MovieDetailsDto> Handle(CreateMovieCommand command, CancellationToken cancellationToken);
}