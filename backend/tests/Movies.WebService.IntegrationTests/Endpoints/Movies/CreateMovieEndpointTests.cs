using System.Net;
using System.Net.Http.Json;
using Movies.WebService.Contracts.Requests;

namespace Movies.WebService.IntegrationTests.Movies;

public sealed class CreateMovieEndpointTests(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
{
    private const string MoviesEndpoint = "/movies-svc/movies";

    [Fact]
    public async Task CreateMovie_WithValidRequest_Returns204NoContent()
    {
        var request = new CreateMoviesRequest
        {
            Title = "Inception",
            YearOfRelease = 2010,
        };

        var response = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CreateMovie_WithValidRequest_ReturnsLocationHeader()
    {
        var request = new CreateMoviesRequest
        {
            Title = "Interstellar",
            YearOfRelease = 2014,
        };

        var response = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);

        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/movies/", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task CreateMovie_WithEmptyTitle_Returns400()
    {
        var request = new CreateMoviesRequest
        {
            Title = string.Empty,
            YearOfRelease = 2010,
        };

        var response = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateMovie_WithTitleExceedingMaxLength_Returns400()
    {
        var request = new CreateMoviesRequest
        {
            Title = new string('A', 257),
            YearOfRelease = 2010,
        };

        var response = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateMovie_WithYearBefore1888_Returns400()
    {
        var request = new CreateMoviesRequest
        {
            Title = "Too Old",
            YearOfRelease = 1887,
        };

        var response = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateMovie_WithYearTooFarInFuture_Returns400()
    {
        var request = new CreateMoviesRequest
        {
            Title = "Too Far Ahead",
            YearOfRelease = DateTime.UtcNow.Year + 6,
        };

        var response = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}