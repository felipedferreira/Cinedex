using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;

namespace Movies.Application.Movies.ListMovies;

internal sealed class ListMoviesHandler(
    IMovieRepository repository,
    ILogger<ListMoviesHandler> logger) : IListMoviesHandler
{
    public async Task<IReadOnlyList<MovieDto>> Handle(ListMoviesQuery query, CancellationToken cancellationToken)
    {
        var movies = await repository.GetAllAsync(cancellationToken);

        logger.LogInformation("Retrieved {MovieCount} movies.", movies.Count);

        return movies.Select(movie => movie.ToDto()).ToList();
    }
}