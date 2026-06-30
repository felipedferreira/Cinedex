using Cinedex.Application.Titles.GetTitleById;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Responses;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Titles;

internal sealed class GetTitleByIdEndpoint(IGetTitleByIdHandler handler) : EndpointWithoutRequest<TitleDetailsResponse>
{
    public override void Configure()
    {
        Get(ApiConstants.Title.RouteById);
        Tags(ApiConstants.Title.Tag);
        AllowAnonymous();
        Description(b => b.WithName(ApiConstants.Title.GetByIdEndpointName));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>(ApiConstants.RouteParameters.Id);
        var title = await handler.HandleAsync(new GetTitleByIdQuery(id), cancellationToken);
        await Send.OkAsync(title.ToResponse(), cancellationToken);
    }
}