using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Constants;
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
            logger.LogWarning(LogMessageConstants.Genre.NotFound, query.Id);
            throw new EntityNotFoundException(nameof(Genre), query.Id);
        }

        logger.LogInformation(LogMessageConstants.Genre.Retrieved, genre.Id);

        return genre.ToDto();
    }
}
