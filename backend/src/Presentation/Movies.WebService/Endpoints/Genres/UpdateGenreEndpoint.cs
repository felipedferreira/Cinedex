using FastEndpoints;
using Movies.Application.Genres.UpdateGenre;
using Movies.WebService.Contracts.Requests;

namespace Movies.WebService.Endpoints.Genres;

internal sealed class UpdateGenreEndpoint(IUpdateGenreHandler handler) : Endpoint<UpdateGenreRequest>
{
    public override void Configure()
    {
        Put("genres/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateGenreRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");
        await handler.Handle(request.ToCommand(id), cancellationToken);
        await Send.AcceptedAtAsync("GetGenreById", new { id }, cancellation: cancellationToken);
    }
}