using Cinedex.Application.Genres.DeleteGenre;
using Cinedex.WebService.Constants;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Genres;

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