using Movies.Application.Genres;

namespace Movies.Application.Movies;

public sealed record MovieDetailsDto(
    Guid Id,
    string Title,
    int YearOfRelease,
    string? Description,
    IReadOnlyList<GenreDto> Genres);