using Movies.Application.Abstractions;

namespace Movies.Application.Movies.GetMovieById;

internal sealed class GetMovieByIdHandler(IMovieRepository repository) : IGetMovieByIdHandler
{
    public async Task<MovieDto?> Handle(GetMovieByIdQuery query, CancellationToken cancellationToken)
    {
        var movie = await repository.GetByIdAsync(query.Id, cancellationToken);

        return movie?.ToDto();
    }
}
