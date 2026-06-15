namespace Movies.Application.Movies.ListMovies;

public interface IListMoviesHandler
{
    Task<IReadOnlyList<MovieDto>> Handle(ListMoviesQuery query, CancellationToken cancellationToken);
}
