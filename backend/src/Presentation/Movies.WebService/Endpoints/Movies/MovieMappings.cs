using Movies.Application.Movies;
using Movies.Application.Movies.CreateMovie;
using Movies.Application.Movies.UpdateMovie;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;
using Movies.WebService.Endpoints.Genres;

namespace Movies.WebService.Endpoints.Movies;

internal static class MovieMappings
{
    public static CreateMovieCommand ToCommand(this CreateMoviesRequest request) =>
        new(request.Title, request.YearOfRelease, request.Description, (request.GenreIds ?? []).ToList());

    public static UpdateMovieCommand ToCommand(this UpdateMoviesRequest request, Guid id) =>
        new(id, request.Title, request.YearOfRelease, request.Description, (request.GenreIds ?? []).ToList());

    public static MovieResponse ToResponse(this MovieDto movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        YearOfRelease = movie.YearOfRelease,
        Description = movie.Description,
    };

    public static MovieDetailsResponse ToResponse(this MovieDetailsDto movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        YearOfRelease = movie.YearOfRelease,
        Description = movie.Description,
        Genres = movie.Genres.Select(genre => genre.ToResponse()).ToList(),
    };

    public static MoviesResponse ToResponse(this IEnumerable<MovieDto> movies) => new()
    {
        Movies = movies.Select(movie => movie.ToResponse()),
    };
}