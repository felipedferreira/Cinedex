namespace Movies.Application.Genres.GetGenreById;

public interface IGetGenreByIdHandler
{
    Task<GenreDto> HandleAsync(GetGenreByIdQuery query, CancellationToken cancellationToken);
}