using FastEndpoints;
using Movies.Application.Genres.ListGenres;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Genres;

internal sealed class GetAllGenresEndpoint(IListGenresHandler handler) : EndpointWithoutRequest<GenresResponse>
{
    public override void Configure()
    {
        Get("genres");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var genres = await handler.Handle(new ListGenresQuery(), cancellationToken);

        await Send.OkAsync(genres.ToResponse(), cancellationToken);
    }
}
