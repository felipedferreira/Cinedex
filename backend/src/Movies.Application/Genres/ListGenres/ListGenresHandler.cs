using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Constants;

namespace Movies.Application.Genres.ListGenres;

internal sealed class ListGenresHandler(
    IGenreRepository repository,
    ILogger<ListGenresHandler> logger) : IListGenresHandler
{
    public async Task<IReadOnlyList<GenreDto>> Handle(ListGenresQuery query, CancellationToken cancellationToken)
    {
        var genres = await repository.GetAllAsync(cancellationToken);

        logger.LogInformation(LogMessageConstants.Genre.RetrievedAll, genres.Count);

        return genres.Select(genre => genre.ToDto()).ToList();
    }
}
