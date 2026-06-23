using FluentValidation;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Exceptions;
using Movies.Domain.Movies;

namespace Movies.Application.Movies.UpdateMovie;

internal sealed class UpdateMovieHandler(
    IMovieRepository repository,
    IGenreRepository genreRepository,
    IValidator<UpdateMovieCommand> validator,
    ILogger<UpdateMovieHandler> logger) : IUpdateMovieHandler
{
    public async Task<MovieDetailsDto> Handle(UpdateMovieCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating movie {MovieId}.", command.Id);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var genres = await genreRepository.GetByIdsAsync(command.GenreIds, cancellationToken);
        GenreLinking.EnsureAllExist(command.GenreIds, genres);

        var movie = new Movie
        {
            Id = command.Id,
            Title = command.Title,
            YearOfRelease = command.YearOfRelease,
            Description = command.Description,
            GenreIds = command.GenreIds.Distinct().ToList(),
        };

        var updated = await repository.UpdateAsync(movie, cancellationToken);

        if (!updated)
        {
            logger.LogWarning("Movie {MovieId} was not found for update.", command.Id);
            throw new EntityNotFoundException(nameof(Movie), command.Id);
        }

        logger.LogInformation("Updated movie {MovieId}.", movie.Id);

        return movie.ToDetailsDto(genres);
    }
}
