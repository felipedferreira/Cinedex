using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Exceptions;
using Movies.Domain.TitleAggregate;

namespace Movies.Application.Titles.GetTitleById;

internal sealed class GetTitleByIdHandler(
    ITitleRepository repository,
    IGenreRepository genreRepository,
    ILogger<GetTitleByIdHandler> logger) : IGetTitleByIdHandler
{
    public async Task<TitleDetailsDto> HandleAsync(GetTitleByIdQuery query, CancellationToken cancellationToken)
    {
        var title = await repository.GetByIdAsync(query.Id, cancellationToken);

        if (title is null)
        {
            logger.LogWarning("Title {TitleId} was not found.", query.Id);
            throw new EntityNotFoundException(nameof(Title), query.Id);
        }

        // Genres live in a separate aggregate; resolve their details by id for the response.
        var genres = await genreRepository.GetByIdsAsync(title.GenreIds, cancellationToken);

        logger.LogInformation("Retrieved title {TitleId}.", title.Id);

        return title.ToDetailsDto(genres);
    }
}
