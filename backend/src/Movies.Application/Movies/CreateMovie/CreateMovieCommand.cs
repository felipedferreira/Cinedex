namespace Movies.Application.Movies.CreateMovie;

public sealed record CreateMovieCommand(string Title, int YearOfRelease, string? Description, IReadOnlyList<Guid> GenreIds);
