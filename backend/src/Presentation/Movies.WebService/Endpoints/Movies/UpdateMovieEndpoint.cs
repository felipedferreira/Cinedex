using FastEndpoints;
using Movies.Application.Movies.UpdateMovie;
using Movies.WebService.Contracts.Requests;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class UpdateMovieEndpoint(IUpdateMovieHandler handler) : Endpoint<UpdateMoviesRequest>
{
    public override void Configure()
    {
        Put("movies/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateMoviesRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");
        await handler.Handle(request.ToCommand(id), cancellationToken);
        await Send.AcceptedAtAsync("GetMovieById", new { id }, cancellation: cancellationToken);
    }
}