using FastEndpoints;
using Movies.Application.Movies.GetMovieById;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class GetMovieByIdEndpoint(IGetMovieByIdHandler handler) : EndpointWithoutRequest<MovieDetailsResponse>
{
    public override void Configure()
    {
        Get("movies/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");
        var movie = await handler.Handle(new GetMovieByIdQuery(id), cancellationToken);
        await Send.OkAsync(movie.ToResponse(), cancellationToken);
    }
}