using Cinedex.Domain.GenreAggregate;

namespace Cinedex.Application.Genres;

internal static class GenreMappings
{
    public static GenreDto ToDto(this Genre genre) =>
        new(genre.Id, genre.Name);
}