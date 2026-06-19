using FastEndpoints;
using Movies.Application.Genres.CreateGenre;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Genres;

internal sealed class CreateGenreEndpoint(ICreateGenreHandler handler) : Endpoint<CreateGenreRequest, GenreResponse>
{
    public override void Configure()
    {
        Post("genres");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateGenreRequest request, CancellationToken cancellationToken)
    {
        var genre = await handler.Handle(request.ToCommand(), cancellationToken);

        await Send.CreatedAtAsync<GetGenreByIdEndpoint>(new { id = genre.Id }, genre.ToResponse(), cancellation: cancellationToken);
    }
}
