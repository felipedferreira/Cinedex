using FluentValidation;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Domain.TitleAggregate;

namespace Movies.Application.Titles.CreateTitle;

internal sealed class CreateTitleHandler(
    ITitleRepository repository,
    IGenreRepository genreRepository,
    IValidator<CreateTitleCommand> validator,
    ILogger<CreateTitleHandler> logger) : ICreateTitleHandler
{
    public async Task<Guid> HandleAsync(CreateTitleCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating title {Title} ({YearOfRelease}).", command.Title, command.YearOfRelease);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var genreIds = command.GenreIds.Distinct().ToList();
        var genres = await genreRepository.GetByIdsAsync(genreIds, cancellationToken);
        GenreLinking.EnsureAllExist(genreIds, genres);

        var title = new Title
        {
            Id = Guid.CreateVersion7(),
            Name = command.Title,
            Type = command.Type,
            YearOfRelease = command.YearOfRelease,
            Description = command.Description,
            GenreIds = genreIds,
        };

        await repository.CreateAsync(title, cancellationToken);

        logger.LogInformation("Created title {TitleId} ({Title}).", title.Id, title.Name);

        return title.Id;
    }
}
