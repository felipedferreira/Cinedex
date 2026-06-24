using FluentValidation;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Constants;
using Movies.Application.Exceptions;
using Movies.Domain.GenreAggregate;

namespace Movies.Application.Genres.UpdateGenre;

internal sealed class UpdateGenreHandler(
    IGenreRepository repository,
    IValidator<UpdateGenreCommand> validator,
    ILogger<UpdateGenreHandler> logger) : IUpdateGenreHandler
{
    public async Task<GenreDto> Handle(UpdateGenreCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(LogMessageConstants.Genre.Updating, command.Id);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var genre = new Genre
        {
            Id = command.Id,
            Name = command.Name,
        };

        var updated = await repository.UpdateAsync(genre, cancellationToken);

        if (!updated)
        {
            logger.LogWarning(LogMessageConstants.Genre.NotFoundForUpdate, command.Id);
            throw new EntityNotFoundException(nameof(Genre), command.Id);
        }

        logger.LogInformation(LogMessageConstants.Genre.Updated, genre.Id);

        return genre.ToDto();
    }
}
