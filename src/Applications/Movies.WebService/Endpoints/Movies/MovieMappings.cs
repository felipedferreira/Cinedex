using Movies.Domain;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Movies;

internal static class MovieMappings
{
    public static MovieResponse ToResponse(this Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        YearOfRelease = movie.YearOfRelease,

        // The domain model does not capture genres yet, so expose an empty
        // collection until the Movie aggregate is extended to store them.
        Genres = [],
    };

    public static MoviesResponse ToResponse(this IEnumerable<Movie> movies) => new()
    {
        Movies = movies.Select(movie => movie.ToResponse()),
    };

    public static Movie ToMovie(this CreateMoviesRequest request) => new()
    {
        Title = request.Title,
        YearOfRelease = request.YearOfRelease,
    };

    public static Movie ToMovie(this UpdateMoviesRequest request, Guid id) => new()
    {
        Id = id,
        Title = request.Title,
        YearOfRelease = request.YearOfRelease,
    };
}
