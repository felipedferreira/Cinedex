namespace Movies.Application.Genres.UpdateGenre;

public interface IUpdateGenreHandler
{
    Task<GenreDto> Handle(UpdateGenreCommand command, CancellationToken cancellationToken);
}