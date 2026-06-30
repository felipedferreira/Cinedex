using Cinedex.Application.Abstractions;
using Cinedex.Application.Exceptions;
using Cinedex.Domain.TitleAggregate;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Titles.UpdateTitle;

internal sealed class UpdateTitleHandler(
    ITitleRepository repository,
    IGenreRepository genreRepository,
    IValidator<UpdateTitleCommand> validator,
    ILogger<UpdateTitleHandler> logger) : IUpdateTitleHandler
{
    public async Task HandleAsync(UpdateTitleCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating title {TitleId}.", command.Id);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var genres = await genreRepository.GetByIdsAsync(command.GenreIds, cancellationToken);
        GenreLinking.EnsureAllExist(command.GenreIds, genres);

        var title = Title.Create(
            command.Id,
            command.Title,
            command.Type,
            command.YearOfRelease,
            command.Description,
            command.GenreIds);

        var updated = await repository.UpdateAsync(title, cancellationToken);

        if (!updated)
        {
            logger.LogWarning("Title {TitleId} was not found for update.", command.Id);
            throw new EntityNotFoundException(nameof(Title), command.Id);
        }

        logger.LogInformation("Updated title {TitleId}.", title.Id);
    }
}