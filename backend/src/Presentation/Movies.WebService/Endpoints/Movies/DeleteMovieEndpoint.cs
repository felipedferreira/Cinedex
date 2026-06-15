using FastEndpoints;
using Movies.Application.Movies.DeleteMovie;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class DeleteMovieEndpoint(IDeleteMovieHandler handler) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("movies/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");

        var deleted = await handler.Handle(new DeleteMovieCommand(id), cancellationToken);

        if (!deleted)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.NoContentAsync(cancellationToken);
    }
}
