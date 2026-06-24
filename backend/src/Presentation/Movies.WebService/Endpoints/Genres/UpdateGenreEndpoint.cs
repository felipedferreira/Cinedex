using FastEndpoints;
using Movies.Application.Genres.UpdateGenre;
using Movies.WebService.Constants;
using Movies.WebService.Contracts.Requests;

namespace Movies.WebService.Endpoints.Genres;

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
        await handler.Handle(request.ToCommand(id), cancellationToken);
        await Send.AcceptedAtAsync(ApiConstants.Genre.GetByIdEndpointName, new { id }, cancellation: cancellationToken);
    }
}
