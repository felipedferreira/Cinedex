using FastEndpoints;
using Movies.Application.Genres.CreateGenre;
using Movies.WebService.Contracts.Requests;

namespace Movies.WebService.Endpoints.Genres;

internal sealed class CreateGenreEndpoint(ICreateGenreHandler handler) : Endpoint<CreateGenreRequest, EmptyResponse>
{
    public override void Configure()
    {
        Post("genres");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateGenreRequest request, CancellationToken cancellationToken)
    {
        var genre = await handler.Handle(request.ToCommand(), cancellationToken);

        await Send.CreatedAtAsync<GetGenreByIdEndpoint>(new { id = genre.Id }, new EmptyResponse(), cancellation: cancellationToken);
    }
}