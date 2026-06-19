namespace Movies.Application.Genres.CreateGenre;

public interface ICreateGenreHandler
{
    Task<GenreDto> Handle(CreateGenreCommand command, CancellationToken cancellationToken);
}
