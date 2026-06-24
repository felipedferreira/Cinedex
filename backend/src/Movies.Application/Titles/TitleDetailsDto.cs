using Movies.Application.Genres;
using Movies.Domain.Enums;

namespace Movies.Application.Titles;

public sealed record TitleDetailsDto(
    Guid Id,
    string Title,
    TitleType Type,
    int YearOfRelease,
    string? Description,
    IReadOnlyList<GenreDto> Genres);