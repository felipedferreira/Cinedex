namespace Movies.Application.Genres.UpdateGenre;

public sealed record UpdateGenreCommand(Guid Id, string Name);