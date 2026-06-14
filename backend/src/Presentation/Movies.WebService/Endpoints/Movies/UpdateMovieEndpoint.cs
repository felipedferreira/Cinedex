using FastEndpoints;
using Movies.Application.Abstractions;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class UpdateMovieEndpoint(IMovieService movieService) : Endpoint<UpdateMoviesRequest, MovieResponse>
{
    public override void Configure()
    {
        Put("movies/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateMoviesRequest request, CancellationToken cancellationToken)
    {
        var id = Route<Guid>("id");

        var updated = await movieService.UpdateAsync(request.ToMovie(id), cancellationToken);

        if (!updated)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        var movie = await movieService.GetByIdAsync(id, cancellationToken);

        if (movie is null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(movie.ToResponse(), cancellationToken);
    }
}
