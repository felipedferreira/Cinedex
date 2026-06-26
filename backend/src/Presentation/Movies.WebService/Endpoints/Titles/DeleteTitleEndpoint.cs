using FastEndpoints;
using Movies.Application.Titles.DeleteTitle;
using Movies.WebService.Constants;

namespace Movies.WebService.Endpoints.Titles;

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
