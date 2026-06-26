using FastEndpoints;
using Movies.Application.Genres.DeleteGenre;
using Movies.WebService.Constants;

namespace Movies.WebService.Endpoints.Genres;

internal sealed class DeleteGenreEndpoint(IDeleteGenreHandler handler) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete(ApiConstants.Genre.RouteById);
        Tags(ApiConstants.Genre.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>(ApiConstants.RouteParameters.Id);
        await handler.HandleAsync(new DeleteGenreCommand(id), cancellationToken);
        await Send.NoContentAsync(cancellationToken);
    }
}
