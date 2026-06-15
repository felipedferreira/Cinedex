using Movies.Application.Abstractions;

namespace Movies.Application.Movies.ListMovies;

internal sealed class ListMoviesHandler(IMovieRepository repository) : IListMoviesHandler
{
    public async Task<IReadOnlyList<MovieDto>> Handle(ListMoviesQuery query, CancellationToken cancellationToken)
    {
        var movies = await repository.GetAllAsync(cancellationToken);

        return movies.Select(movie => movie.ToDto()).ToList();
    }
}
