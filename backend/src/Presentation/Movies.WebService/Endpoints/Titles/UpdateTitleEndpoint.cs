using FastEndpoints;
using Movies.Application.Titles.UpdateTitle;
using Movies.WebService.Constants;
using Movies.WebService.Contracts.Requests;

namespace Movies.WebService.Endpoints.Titles;

internal sealed class UpdateTitleEndpoint(IUpdateTitleHandler handler) : Endpoint<UpdateTitlesRequest>
{
    public override void Configure()
    {
        Put(ApiConstants.Title.RouteById);
        Tags(ApiConstants.Title.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateTitlesRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>(ApiConstants.RouteParameters.Id);
        await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        await Send.AcceptedAtAsync(ApiConstants.Title.GetByIdEndpointName, new { id }, cancellation: cancellationToken);
    }
}
