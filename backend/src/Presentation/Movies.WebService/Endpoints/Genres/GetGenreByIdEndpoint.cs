using FastEndpoints;
using Movies.Application.Genres.GetGenreById;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Genres;

internal sealed class GetGenreByIdEndpoint(IGetGenreByIdHandler handler) : EndpointWithoutRequest<GenreResponse>
{
    public override void Configure()
    {
        Get("genres/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");
        var genre = await handler.Handle(new GetGenreByIdQuery(id), cancellationToken);
        await Send.OkAsync(genre.ToResponse(), cancellationToken);
    }
}
