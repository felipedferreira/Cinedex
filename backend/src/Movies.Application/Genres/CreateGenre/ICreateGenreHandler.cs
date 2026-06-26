namespace Movies.Application.Genres.CreateGenre;

public interface ICreateGenreHandler
{
    Task<Guid> HandleAsync(CreateGenreCommand command, CancellationToken cancellationToken);
}
