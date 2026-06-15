using Movies.Application.Movies;
using Movies.Application.Movies.CreateMovie;
using Movies.Application.Movies.UpdateMovie;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Movies;

internal static class MovieMappings
{
    public static CreateMovieCommand ToCommand(this CreateMoviesRequest request) =>
        new(request.Title, request.YearOfRelease, null);

    public static UpdateMovieCommand ToCommand(this UpdateMoviesRequest request, Guid id) =>
        new(id, request.Title, request.YearOfRelease, null);

    public static MovieResponse ToResponse(this MovieDto movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        YearOfRelease = movie.YearOfRelease,

        // The domain model does not capture genres yet, so expose an empty
        // collection until the Movie aggregate is extended to store them.
        Genres = [],
    };

    public static MoviesResponse ToResponse(this IEnumerable<MovieDto> movies) => new()
    {
        Movies = movies.Select(movie => movie.ToResponse()),
    };
}
