using Movies.Application.Genres;
using Movies.Domain;

namespace Movies.Application.Movies;

internal static class MovieMappings
{
    public static MovieDto ToDto(this Movie movie) =>
        new(
            movie.Id,
            movie.Title,
            movie.YearOfRelease,
            movie.Description,
            movie.Genres.Select(genre => genre.ToDto()).ToList());
}
