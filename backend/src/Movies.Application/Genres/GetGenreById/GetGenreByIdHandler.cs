using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Exceptions;
using Movies.Domain.GenreAggregate;

namespace Movies.Application.Genres.GetGenreById;

internal sealed class GetGenreByIdHandler(
    IGenreRepository repository,
    ILogger<GetGenreByIdHandler> logger) : IGetGenreByIdHandler
{
    public async Task<GenreDto> Handle(GetGenreByIdQuery query, CancellationToken cancellationToken)
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