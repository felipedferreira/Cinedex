using FluentValidation;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Constants;
using Movies.Domain.GenreAggregate;

namespace Movies.Application.Genres.CreateGenre;

internal sealed class CreateGenreHandler(
    IGenreRepository repository,
    IValidator<CreateGenreCommand> validator,
    ILogger<CreateGenreHandler> logger) : ICreateGenreHandler
{
    public async Task<GenreDto> Handle(CreateGenreCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(LogMessageConstants.Genre.Creating, command.Name);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var genre = new Genre
        {
            Name = command.Name,
        };

        var created = await repository.CreateAsync(genre, cancellationToken);

        logger.LogInformation(LogMessageConstants.Genre.Created, created.Id, created.Name);

        return created.ToDto();
    }
}
