namespace Movies.Application.Genres.DeleteGenre;

public interface IDeleteGenreHandler
{
    Task Handle(DeleteGenreCommand command, CancellationToken cancellationToken);
}
