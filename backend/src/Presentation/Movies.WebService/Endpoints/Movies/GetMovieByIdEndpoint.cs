using FastEndpoints;
using Movies.Application.Abstractions;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class GetMovieByIdEndpoint(IMovieService movieService) : EndpointWithoutRequest<MovieResponse>
{
    public override void Configure()
    {
        Get("movies/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");

        var movie = await movieService.GetByIdAsync(id, cancellationToken);

        if (movie is null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(movie.ToResponse(), cancellationToken);
    }
}
