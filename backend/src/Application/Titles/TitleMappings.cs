using Cinedex.Application.Genres;
using Cinedex.Domain.GenreAggregate;
using Cinedex.Domain.TitleAggregate;

namespace Cinedex.Application.Titles;

internal static class TitleMappings
{
    public static TitleDto ToDto(this Title title) =>
        new(title.Id, title.Name, title.Type, title.YearOfRelease, title.Description);

    public static TitleDetailsDto ToDetailsDto(this Title title, IReadOnlyList<Genre> genres) =>
        new(
            title.Id,
            title.Name,
            title.Type,
            title.YearOfRelease,
            title.Description,
            genres.Select(genre => genre.ToDto()).ToList());
}