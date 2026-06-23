using FastEndpoints;
using Movies.Application.Movies.CreateMovie;
using Movies.WebService.Contracts.Requests;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class CreateMovieEndpoint(ICreateMovieHandler handler) : Endpoint<CreateMoviesRequest>
{
    public override void Configure()
    {
        Post("movies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateMoviesRequest request, CancellationToken cancellationToken)
    {
        var movie = await handler.Handle(request.ToCommand(), cancellationToken);

        HttpContext.Response.StatusCode = 204;
        HttpContext.Response.Headers.Location = $"/movies/{movie.Id}";
        await HttpContext.Response.CompleteAsync();
    }
}