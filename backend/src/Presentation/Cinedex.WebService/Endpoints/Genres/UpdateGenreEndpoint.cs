using Cinedex.Application.Genres.UpdateGenre;
using Cinedex.WebService.Constants;
using Cinedex.WebService.Contracts.Requests;
using FastEndpoints;

namespace Cinedex.WebService.Endpoints.Genres;

internal sealed class UpdateGenreEndpoint(IUpdateGenreHandler handler) : Endpoint<UpdateGenreRequest>
{
    public override void Configure()
    {
        Put(ApiConstants.Genre.RouteById);
        Tags(ApiConstants.Genre.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateGenreRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>(ApiConstants.RouteParameters.Id);
        await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        await Send.AcceptedAtAsync(ApiConstants.Genre.GetByIdEndpointName, new { id }, cancellation: cancellationToken);
    }
}