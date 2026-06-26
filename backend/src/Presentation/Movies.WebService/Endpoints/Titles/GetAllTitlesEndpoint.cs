using FastEndpoints;
using Movies.Application.Titles.ListTitles;
using Movies.WebService.Constants;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Titles;

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
