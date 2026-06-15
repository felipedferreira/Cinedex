namespace Movies.Application.Movies.UpdateMovie;

public sealed record UpdateMovieCommand(Guid Id, string Title, int YearOfRelease, string? Description);
