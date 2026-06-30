using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;

namespace Movies.Application.Genres.ListGenres;

internal sealed class ListGenresHandler(
    IGenreRepository repository,
    ILogger<ListGenresHandler> logger) : IListGenresHandler
{
    public async Task<IReadOnlyList<GenreDto>> HandleAsync(ListGenresQuery query, CancellationToken cancellationToken)
    {
        var genres = await repository.GetAllAsync(cancellationToken);

        logger.LogInformation("Retrieved {GenreCount} genres.", genres.Count);

        return genres.Select(genre => genre.ToDto()).ToList();
    }
}
