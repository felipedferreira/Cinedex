using Movies.Application.Genres;
using Movies.Application.Genres.CreateGenre;
using Movies.Application.Genres.UpdateGenre;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Genres;

internal static class GenreMappings
{
    public static CreateGenreCommand ToCommand(this CreateGenreRequest request) =>
        new(request.Name, request.Description);

    public static UpdateGenreCommand ToCommand(this UpdateGenreRequest request, Guid id) =>
        new(id, request.Name, request.Description);

    public static GenreResponse ToResponse(this GenreDto genre) => new()
    {
        Id = genre.Id,
        Name = genre.Name,
        Description = genre.Description,
    };

    public static GenresResponse ToResponse(this IEnumerable<GenreDto> genres) => new()
    {
        Genres = genres.Select(genre => genre.ToResponse()),
    };
}
