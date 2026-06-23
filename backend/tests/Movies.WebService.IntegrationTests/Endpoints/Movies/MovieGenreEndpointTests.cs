using System.Net;
using System.Net.Http.Json;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.IntegrationTests.Movies;

public sealed class MovieGenreEndpointTests(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
{
    private const string MoviesEndpoint = "/movies-svc/movies";
    private const string GenresEndpoint = "/movies-svc/genres";

    [Fact]
    public async Task CreateMovie_WithDescription_PersistsDescription()
    {
        var request = new CreateMoviesRequest
        {
            Title = "Blade Runner 2049",
            YearOfRelease = 2017,
            Description = "A young blade runner discovers a long-buried secret.",
        };

        var createResponse = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);
        Assert.NotNull(createResponse.Headers.Location);

        var locationUri = createResponse.Headers.Location.ToString();
        var idString = locationUri.Substring("/movies/".Length);
        var id = Guid.Parse(idString);

        var fetched = await fixture.Client.GetFromJsonAsync<MovieResponse>($"{MoviesEndpoint}/{id}");

        Assert.NotNull(fetched);
        Assert.Equal(request.Description, fetched.Description);
    }

    [Fact]
    public async Task CreateMovie_WithGenreIds_LinksGenres()
    {
        var genres = await GetSeededGenresAsync();
        var sciFi = genres.Single(genre => genre.Name == "SciFi");
        var thriller = genres.Single(genre => genre.Name == "Thriller");

        var request = new CreateMoviesRequest
        {
            Title = "Minority Report",
            YearOfRelease = 2002,
            GenreIds = [sciFi.Id, thriller.Id],
        };

        var createResponse = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);
        Assert.NotNull(createResponse.Headers.Location);

        var locationUri = createResponse.Headers.Location.ToString();
        var idString = locationUri.Substring("/movies/".Length);
        var id = Guid.Parse(idString);

        var fetched = await fixture.Client.GetFromJsonAsync<MovieDetailsResponse>($"{MoviesEndpoint}/{id}");

        Assert.NotNull(fetched);
        Assert.Equal(2, fetched.Genres.Count());
        Assert.Contains(fetched.Genres, genre => genre.Id == sciFi.Id && genre.Name == "SciFi");
        Assert.Contains(fetched.Genres, genre => genre.Id == thriller.Id);
    }

    [Fact]
    public async Task UpdateMovie_ReplacesLinkedGenres()
    {
        var genres = await GetSeededGenresAsync();
        var action = genres.Single(genre => genre.Name == "Action");
        var drama = genres.Single(genre => genre.Name == "Drama");
        var crime = genres.Single(genre => genre.Name == "Crime");

        var createResponse = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, new CreateMoviesRequest
        {
            Title = "Heat",
            YearOfRelease = 1995,
            GenreIds = [action.Id, drama.Id],
        });
        Assert.NotNull(createResponse.Headers.Location);

        var locationUri = createResponse.Headers.Location.ToString();
        var idString = locationUri.Substring("/movies/".Length);
        var id = Guid.Parse(idString);

        var updateResponse = await fixture.Client.PutAsJsonAsync($"{MoviesEndpoint}/{id}", new UpdateMoviesRequest
        {
            Title = "Heat",
            YearOfRelease = 1995,
            GenreIds = [crime.Id],
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var fetched = await fixture.Client.GetFromJsonAsync<MovieDetailsResponse>($"{MoviesEndpoint}/{id}");

        Assert.NotNull(fetched);
        var genre = Assert.Single(fetched.Genres);
        Assert.Equal(crime.Id, genre.Id);
    }

    [Fact]
    public async Task CreateMovie_WithUnknownGenreId_Returns404()
    {
        var request = new CreateMoviesRequest
        {
            Title = "Ghost Genre",
            YearOfRelease = 2020,
            GenreIds = [Guid.NewGuid()],
        };

        var response = await fixture.Client.PostAsJsonAsync(MoviesEndpoint, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<IReadOnlyList<GenreResponse>> GetSeededGenresAsync()
    {
        var response = await fixture.Client.GetFromJsonAsync<GenresResponse>(GenresEndpoint);
        Assert.NotNull(response);
        return response.Genres.ToList();
    }
}