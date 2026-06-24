using FastEndpoints;
using Movies.Application.Genres.CreateGenre;
using Movies.WebService.Constants;
using Movies.WebService.Contracts.Requests;

namespace Movies.WebService.Endpoints.Genres;

internal sealed class CreateGenreEndpoint(ICreateGenreHandler handler) : Endpoint<CreateGenreRequest, EmptyResponse>
{
    public override void Configure()
    {
        Post(ApiConstants.Genre.Route);
        Tags(ApiConstants.Genre.Tag);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateGenreRequest request, CancellationToken cancellationToken)
    {
        var genre = await handler.Handle(request.ToCommand(), cancellationToken);

        await Send.CreatedAtAsync(ApiConstants.Genre.GetByIdEndpointName, new { id = genre.Id }, default!, cancellation: cancellationToken);
    }
}
