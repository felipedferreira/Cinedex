namespace Movies.Application.Genres.GetGenreById;

public interface IGetGenreByIdHandler
{
    Task<GenreDto> Handle(GetGenreByIdQuery query, CancellationToken cancellationToken);
}
