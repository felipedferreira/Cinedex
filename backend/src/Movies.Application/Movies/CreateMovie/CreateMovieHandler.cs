using FluentValidation;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Domain.Movies;

namespace Movies.Application.Movies.CreateMovie;

internal sealed class CreateMovieHandler(
    IMovieRepository repository,
    IGenreRepository genreRepository,
    IValidator<CreateMovieCommand> validator,
    ILogger<CreateMovieHandler> logger) : ICreateMovieHandler
{
    public async Task<MovieDetailsDto> Handle(CreateMovieCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating movie {Title} ({YearOfRelease}).", command.Title, command.YearOfRelease);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var genres = await genreRepository.GetByIdsAsync(command.GenreIds, cancellationToken);
        GenreLinking.EnsureAllExist(command.GenreIds, genres);

        var movie = new Movie
        {
            Title = command.Title,
            YearOfRelease = command.YearOfRelease,
            Description = command.Description,
            GenreIds = command.GenreIds.Distinct().ToList(),
        };

        var created = await repository.CreateAsync(movie, cancellationToken);

        logger.LogInformation("Created movie {MovieId} ({Title}).", created.Id, created.Title);

        return created.ToDetailsDto(genres);
    }
}
