namespace Cinedex.Application.Genres.ListGenres;

public interface IListGenresHandler
{
    Task<IReadOnlyList<GenreDto>> HandleAsync(ListGenresQuery query, CancellationToken cancellationToken);
}