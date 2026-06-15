namespace Movies.Application.Movies.GetMovieById;

public interface IGetMovieByIdHandler
{
    Task<MovieDto> Handle(GetMovieByIdQuery query, CancellationToken cancellationToken);
}
