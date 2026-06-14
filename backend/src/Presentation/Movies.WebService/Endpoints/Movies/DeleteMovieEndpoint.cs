using FastEndpoints;
using Movies.Application.Abstractions;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class DeleteMovieEndpoint(IMovieService movieService) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("movies/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");

        var deleted = await movieService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.NoContentAsync(cancellationToken);
    }
}
