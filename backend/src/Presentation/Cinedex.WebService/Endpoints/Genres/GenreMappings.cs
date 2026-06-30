using Cinedex.Application.Genres;
using Cinedex.Application.Genres.CreateGenre;
using Cinedex.Application.Genres.UpdateGenre;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;

namespace Cinedex.WebService.Endpoints.Genres;

internal static class GenreMappings
{
    public static CreateGenreCommand ToCommand(this CreateGenreRequest request) =>
        new(request.Name);

    public static UpdateGenreCommand ToCommand(this UpdateGenreRequest request, Guid id) =>
        new(id, request.Name);

    public static GenreResponse ToResponse(this GenreDto genre) => new()
    {
        Id = genre.Id,
        Name = genre.Name,
    };

    public static GenresResponse ToResponse(this IEnumerable<GenreDto> genres) => new()
    {
        Genres = genres.Select(genre => genre.ToResponse()),
    };
}