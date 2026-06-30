using Cinedex.Application.Titles.DeleteTitle;
using Cinedex.WebService.Constants;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Titles;

internal sealed class DeleteTitleEndpoint(IDeleteTitleHandler handler) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete(ApiConstants.Title.RouteById);
        Tags(ApiConstants.Title.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>(ApiConstants.RouteParameters.Id);
        await handler.HandleAsync(new DeleteTitleCommand(id), cancellationToken);
        await Send.NoContentAsync(cancellationToken);
    }
}