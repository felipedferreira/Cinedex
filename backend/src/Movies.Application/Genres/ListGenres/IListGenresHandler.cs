namespace Movies.Application.Genres.ListGenres;

public interface IListGenresHandler
{
    Task<IReadOnlyList<GenreDto>> Handle(ListGenresQuery query, CancellationToken cancellationToken);
}