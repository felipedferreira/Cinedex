using Cinedex.Application.Genres;
using Cinedex.Domain.Enums;

namespace Cinedex.Application.Titles;

public sealed record TitleDetailsDto(
    Guid Id,
    string Title,
    TitleType Type,
    int YearOfRelease,
    string? Description,
    IReadOnlyList<GenreDto> Genres);