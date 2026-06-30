namespace Movies.Application.Genres.UpdateGenre;

public interface IUpdateGenreHandler
{
    Task HandleAsync(UpdateGenreCommand command, CancellationToken cancellationToken);
}
