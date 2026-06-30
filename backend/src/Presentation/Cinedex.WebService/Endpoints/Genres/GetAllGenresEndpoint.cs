using Cinedex.Application.Genres.ListGenres;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Responses;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Genres;

internal sealed class GetAllGenresEndpoint(IListGenresHandler handler) : EndpointWithoutRequest<GenresResponse>
{
    public override void Configure()
    {
        Get(ApiConstants.Genre.Route);
        Tags(ApiConstants.Genre.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var genres = await handler.HandleAsync(new ListGenresQuery(), cancellationToken);

        await Send.OkAsync(genres.ToResponse(), cancellationToken);
    }
}