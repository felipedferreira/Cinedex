using Cinedex.Application.Titles.CreateTitle;
using Cinedex.WebService.Constants;
using FastEndpoints;

using Oceanus.WebService.Contracts.Requests;
using Oceanus.WebService.Contracts.Responses;
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