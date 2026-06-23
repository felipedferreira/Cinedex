using Movies.Domain.Genres;

namespace Movies.Application.Genres;

internal static class GenreMappings
{
    public static GenreDto ToDto(this Genre genre) =>
        new(genre.Id, genre.Name, genre.Description);
}
