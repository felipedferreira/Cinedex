using FluentValidation;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Domain;

namespace Movies.Application.Genres.CreateGenre;

internal sealed class CreateGenreHandler(
    IGenreRepository repository,
    IValidator<CreateGenreCommand> validator,
    ILogger<CreateGenreHandler> logger) : ICreateGenreHandler
{
    public async Task<GenreDto> Handle(CreateGenreCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating genre {Name}.", command.Name);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var genre = new Genre
        {
            Name = command.Name,
            Description = command.Description,
        };

        var created = await repository.CreateAsync(genre, cancellationToken);

        logger.LogInformation("Created genre {GenreId} ({Name}).", created.Id, created.Name);

        return created.ToDto();
    }
}
