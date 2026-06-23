using Movies.Application.Genres;
using Movies.Domain.GenreAggregate;
using Movies.Domain.MovieAggregate;

namespace Movies.Application.Movies;

internal static class MovieMappings
{
    public static MovieDto ToDto(this Movie movie) =>
        new(movie.Id, movie.Title, movie.YearOfRelease, movie.Description);

    public static MovieDetailsDto ToDetailsDto(this Movie movie, IReadOnlyList<Genre> genres) =>
        new(
            movie.Id,
            movie.Title,
            movie.YearOfRelease,
            movie.Description,
            genres.Select(genre => genre.ToDto()).ToList());
}