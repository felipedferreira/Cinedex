using Movies.Domain.GenreAggregate;

namespace Movies.Application.Genres;

internal static class GenreMappings
{
    public static GenreDto ToDto(this Genre genre) =>
        new(genre.Id, genre.Name);
}