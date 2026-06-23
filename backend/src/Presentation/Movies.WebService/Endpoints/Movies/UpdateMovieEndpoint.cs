using FastEndpoints;
using Movies.Application.Movies.UpdateMovie;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class UpdateMovieEndpoint(IUpdateMovieHandler handler) : Endpoint<UpdateMoviesRequest, MovieDetailsResponse>
{
    public override void Configure()
    {
        Put("movies/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateMoviesRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");
        var movie = await handler.Handle(request.ToCommand(id), cancellationToken);
        await Send.OkAsync(movie.ToResponse(), cancellationToken);
    }
}