using FastEndpoints;
using Movies.Application.Genres.DeleteGenre;

namespace Movies.WebService.Endpoints.Genres;

internal sealed class DeleteGenreEndpoint(IDeleteGenreHandler handler) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("genres/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");
        await handler.Handle(new DeleteGenreCommand(id), cancellationToken);
        await Send.NoContentAsync(cancellationToken);
    }
}