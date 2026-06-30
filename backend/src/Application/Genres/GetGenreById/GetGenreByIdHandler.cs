using Cinedex.Application.Abstractions;
using Cinedex.Application.Exceptions;
using Cinedex.Domain.GenreAggregate;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Genres.GetGenreById;

internal sealed class GetGenreByIdHandler(
    IGenreRepository repository,
    ILogger<GetGenreByIdHandler> logger) : IGetGenreByIdHandler
{
    public async Task<GenreDto> HandleAsync(GetGenreByIdQuery query, CancellationToken cancellationToken)
    {
        var genre = await repository.GetByIdAsync(query.Id, cancellationToken);

        if (genre is null)
        {
            logger.LogWarning("Genre {GenreId} was not found.", query.Id);
            throw new EntityNotFoundException(nameof(Genre), query.Id);
        }

        logger.LogInformation("Retrieved genre {GenreId}.", genre.Id);

        return genre.ToDto();
    }
}