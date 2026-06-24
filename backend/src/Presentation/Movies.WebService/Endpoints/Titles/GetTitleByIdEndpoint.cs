using FastEndpoints;
using Movies.Application.Titles.GetTitleById;
using Movies.WebService.Constants;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Titles;

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
        var title = await handler.Handle(new GetTitleByIdQuery(id), cancellationToken);
        await Send.OkAsync(title.ToResponse(), cancellationToken);
    }
}
