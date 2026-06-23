namespace Movies.Application.Movies.GetMovieById;

public interface IGetMovieByIdHandler
{
    Task<MovieDetailsDto> Handle(GetMovieByIdQuery query, CancellationToken cancellationToken);
}