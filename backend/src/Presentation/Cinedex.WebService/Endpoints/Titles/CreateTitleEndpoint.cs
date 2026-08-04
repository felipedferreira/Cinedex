using Cinedex.Application.Titles.CreateTitle;
using Cinedex.WebService.Constants;
using FastEndpoints;
using FoundryOceanus.WebService.Contracts.Requests;

namespace Cinedex.WebService.Endpoints.Titles;

internal sealed class CreateTitleEndpoint(ICreateTitleHandler handler) : Endpoint<CreateTitlesRequest, EmptyResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Title.Route);
        Tags(ApiConstants.Title.Tag);
    }

    public override async Task HandleAsync(CreateTitlesRequest request, CancellationToken cancellationToken)
    {
        var titleId = await handler.HandleAsync(request.ToCommand(), cancellationToken);

        await Send.CreatedAtAsync(ApiConstants.Title.GetByIdEndpointName, new { id = titleId }, default!, cancellation: cancellationToken);
    }
}