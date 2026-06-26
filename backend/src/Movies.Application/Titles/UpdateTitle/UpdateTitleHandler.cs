using FluentValidation;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Exceptions;
using Movies.Domain.TitleAggregate;

namespace Movies.Application.Titles.UpdateTitle;

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

        var title = new Title
        {
            Id = command.Id,
            Name = command.Title,
            Type = command.Type,
            YearOfRelease = command.YearOfRelease,
            Description = command.Description,
            GenreIds = command.GenreIds.Distinct().ToList(),
        };

        var updated = await repository.UpdateAsync(title, cancellationToken);

        if (!updated)
        {
            logger.LogWarning("Title {TitleId} was not found for update.", command.Id);
            throw new EntityNotFoundException(nameof(Title), command.Id);
        }

        logger.LogInformation("Updated title {TitleId}.", title.Id);
    }
}
