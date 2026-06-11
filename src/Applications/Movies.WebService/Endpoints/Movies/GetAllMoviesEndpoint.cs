using FastEndpoints;
using Movies.Application.Abstractions;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class GetAllMoviesEndpoint(IMovieService movieService) : EndpointWithoutRequest<MoviesResponse>
{
    public override void Configure()
    {
        Get("movies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var movies = await movieService.GetAllAsync(cancellationToken);

        await Send.OkAsync(movies.ToResponse(), cancellationToken);
    }
}
