using FastEndpoints;
using Movies.Application.Abstractions;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.Endpoints.Movies;

internal sealed class CreateMovieEndpoint(IMovieService movieService) : Endpoint<CreateMoviesRequest, MovieResponse>
{
    public override void Configure()
    {
        Post("movies");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateMoviesRequest request, CancellationToken cancellationToken)
    {
        var movie = await movieService.CreateAsync(request.ToMovie(), cancellationToken);

        await Send.CreatedAtAsync<GetMovieByIdEndpoint>(new { id = movie.Id }, movie.ToResponse(), cancellation: cancellationToken);
    }
}
