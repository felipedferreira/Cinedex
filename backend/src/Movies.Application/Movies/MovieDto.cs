using Movies.Application.Genres;

namespace Movies.Application.Movies;

public sealed record MovieDto(Guid Id, string Title, int YearOfRelease, string? Description, IReadOnlyList<GenreDto> Genres);
