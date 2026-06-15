using FastEndpoints;
using Movies.Application.Movies.ListMovies;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class GetAllMoviesEndpoint(IListMoviesHandler handler) : EndpointWithoutRequest<MoviesResponse>
{
    public override void Configure()
    {
        Get("movies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var movies = await handler.Handle(new ListMoviesQuery(), cancellationToken);

        await Send.OkAsync(movies.ToResponse(), cancellationToken);
    }
}
