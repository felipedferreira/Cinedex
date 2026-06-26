namespace Movies.Application.Genres.DeleteGenre;

public interface IDeleteGenreHandler
{
    Task HandleAsync(DeleteGenreCommand command, CancellationToken cancellationToken);
}