using FastEndpoints;
using Movies.Application.Genres.UpdateGenre;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Genres;

internal sealed class UpdateGenreEndpoint(IUpdateGenreHandler handler) : Endpoint<UpdateGenreRequest, GenreResponse>
{
    public override void Configure()
    {
        Put("genres/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateGenreRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");
        var genre = await handler.Handle(request.ToCommand(id), cancellationToken);
        await Send.OkAsync(genre.ToResponse(), cancellationToken);
    }
}