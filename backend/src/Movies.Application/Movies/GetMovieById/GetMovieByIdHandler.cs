using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions;
using Movies.Application.Exceptions;
using Movies.Domain.MovieAggregate;

namespace Movies.Application.Movies.GetMovieById;

internal sealed class GetMovieByIdHandler(
    IMovieRepository repository,
    IGenreRepository genreRepository,
    ILogger<GetMovieByIdHandler> logger) : IGetMovieByIdHandler
{
    public async Task<MovieDetailsDto> Handle(GetMovieByIdQuery query, CancellationToken cancellationToken)
    {
        var movie = await repository.GetByIdAsync(query.Id, cancellationToken);

        if (movie is null)
        {
            logger.LogWarning("Movie {MovieId} was not found.", query.Id);
            throw new EntityNotFoundException(nameof(Movie), query.Id);
        }

        // Genres live in a separate aggregate; resolve their details by id for the response.
        var genres = await genreRepository.GetByIdsAsync(movie.GenreIds, cancellationToken);

        logger.LogInformation("Retrieved movie {MovieId}.", movie.Id);

        return movie.ToDetailsDto(genres);
    }
}