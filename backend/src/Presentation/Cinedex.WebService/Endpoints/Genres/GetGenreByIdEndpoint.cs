using Cinedex.Application.Genres.GetGenreById;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Responses;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Genres;

internal sealed class GetGenreByIdEndpoint(IGetGenreByIdHandler handler) : EndpointWithoutRequest<GenreResponse>
{
    public override void Configure()
    {
        Get(ApiConstants.Genre.RouteById);
        Tags(ApiConstants.Genre.Tag);
        AllowAnonymous();
        Description(b => b.WithName(ApiConstants.Genre.GetByIdEndpointName));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>(ApiConstants.RouteParameters.Id);
        var genre = await handler.HandleAsync(new GetGenreByIdQuery(id), cancellationToken);
        await Send.OkAsync(genre.ToResponse(), cancellationToken);
    }
}