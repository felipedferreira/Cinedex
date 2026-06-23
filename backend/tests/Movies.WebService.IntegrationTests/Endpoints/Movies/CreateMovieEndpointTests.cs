using System.Net;
using System.Net.Http.Json;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.IntegrationTests.Movies;

public sealed class CreateMovieEndpointTests(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
{
    private const string MoviesEndpoint = "/movies-svc/movies";

    [Fact]
    public async Task CreateMovie_WithValidRequest_Returns201Created()
    {
        var request = new CreateMoviesRequest
        {
            Title = "Inception",
            YearOfRelease = 2010,
        };

        var response = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateMovie_WithValidRequest_ReturnsMovieInBody()
    {
        var request = new CreateMoviesRequest
        {
            Title = "The Dark Knight",
            YearOfRelease = 2008,
        };

        var response = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);

        var body = await response.Content.ReadFromJsonAsync<MovieDetailsResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(request.Title, body.Title);
        Assert.Equal(request.YearOfRelease, body.YearOfRelease);
        Assert.Empty(body.Genres);
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

        var body = await response.Content.ReadFromJsonAsync<MovieResponse>();
        Assert.NotNull(body);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/movies/{body.Id}", response.Headers.Location.ToString());
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
