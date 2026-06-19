namespace Movies.Application.Genres.CreateGenre;

public sealed record CreateGenreCommand(string Name, string? Description);
