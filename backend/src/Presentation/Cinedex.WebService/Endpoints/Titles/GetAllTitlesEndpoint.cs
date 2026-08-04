using Cinedex.Application.Titles.ListTitles;
using Cinedex.WebService.Constants;
using FastEndpoints;

using Oceanus.WebService.Contracts.Requests;
using Oceanus.WebService.Contracts.Responses;
namespace Cinedex.WebService.Endpoints.Titles;

internal sealed class GetAllTitlesEndpoint(IListTitlesHandler handler) : EndpointWithoutRequest<TitlesResponse>
{
    public override void Configure()
    {
        Get(ApiConstants.Title.Route);
        Tags(ApiConstants.Title.Tag);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var titles = await handler.HandleAsync(new ListTitlesQuery(), cancellationToken);

        await Send.OkAsync(titles.ToResponse(), cancellationToken);
    }
}