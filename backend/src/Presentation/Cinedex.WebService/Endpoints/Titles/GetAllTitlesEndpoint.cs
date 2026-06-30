using Cinedex.Application.Titles.ListTitles;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Responses;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Titles;

internal sealed class GetAllTitlesEndpoint(IListTitlesHandler handler) : EndpointWithoutRequest<TitlesResponse>
{
    public override void Configure()
    {
        Get(ApiConstants.Title.Route);
        Tags(ApiConstants.Title.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var titles = await handler.HandleAsync(new ListTitlesQuery(), cancellationToken);

        await Send.OkAsync(titles.ToResponse(), cancellationToken);
    }
}