using Movies.Application.Abstractions;
using Movies.Application.Exceptions;
using Movies.Domain;

namespace Movies.Application.Movies.GetMovieById;

internal sealed class GetMovieByIdHandler(IMovieRepository repository) : IGetMovieByIdHandler
{
    public async Task<MovieDto> Handle(GetMovieByIdQuery query, CancellationToken cancellationToken)
    {
        var movie = await repository.GetByIdAsync(query.Id, cancellationToken);

        if (movie is null)
        {
            throw new EntityNotFoundException(nameof(Movie), query.Id);
        }

        return movie.ToDto();
    }
}
