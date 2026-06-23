using System.Net;
using System.Net.Http.Json;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;

namespace Movies.WebService.IntegrationTests.Genres;

public sealed class GenreEndpointTests(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
{
    private const string GenresEndpoint = "/movies-svc/genres";

    [Fact]
    public async Task GetAllGenres_ReturnsSeededGenres()
    {
        var response = await fixture.Client.GetFromJsonAsync<GenresResponse>(GenresEndpoint);

        Assert.NotNull(response);
        var names = response.Genres.Select(genre => genre.Name).ToList();
        Assert.True(names.Count >= 17);
        Assert.Contains("Action", names);
        Assert.Contains("Western", names);
    }

    [Fact]
    public async Task CreateGenre_Returns204AndIsRetrievable()
    {
        var request = new CreateGenreRequest
        {
            Name = "Noir",
        };

        var createResponse = await fixture.Client.PostAsJsonAsync(GenresEndpoint, request);
        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        Assert.NotNull(createResponse.Headers.Location);
        var locationUri = createResponse.Headers.Location.ToString();
        Assert.StartsWith("/genres/", locationUri);

        var idString = locationUri.Substring("/genres/".Length);
        Assert.True(Guid.TryParse(idString, out var id));

        var fetched = await fixture.Client.GetFromJsonAsync<GenreResponse>($"{GenresEndpoint}/{id}");
        Assert.NotNull(fetched);
        Assert.Equal(request.Name, fetched.Name);
    }

    [Fact]
    public async Task CreateGenre_WithEmptyName_Returns400()
    {
        var request = new CreateGenreRequest { Name = string.Empty };

        var response = await fixture.Client.PostAsJsonAsync(GenresEndpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGenre_ChangesName()
    {
        var created = await CreateGenreAsync("Heist");

        var update = new UpdateGenreRequest { Name = "Heist Thriller" };
        var updateResponse = await fixture.Client.PutAsJsonAsync($"{GenresEndpoint}/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var fetched = await fixture.Client.GetFromJsonAsync<GenreResponse>($"{GenresEndpoint}/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal("Heist Thriller", fetched.Name);
    }

    [Fact]
    public async Task DeleteGenre_RemovesGenre()
    {
        var created = await CreateGenreAsync("Disaster");

        var deleteResponse = await fixture.Client.DeleteAsync($"{GenresEndpoint}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await fixture.Client.GetAsync($"{GenresEndpoint}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetGenre_WithUnknownId_Returns404()
    {
        var response = await fixture.Client.GetAsync($"{GenresEndpoint}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<GenreResponse> CreateGenreAsync(string name)
    {
        var createResponse = await fixture.Client.PostAsJsonAsync(GenresEndpoint, new CreateGenreRequest
        {
            Name = name,
        });
        Assert.NotNull(createResponse.Headers.Location);

        var locationUri = createResponse.Headers.Location.ToString();
        var idString = locationUri.Substring("/genres/".Length);
        var id = Guid.Parse(idString);

        var created = await fixture.Client.GetFromJsonAsync<GenreResponse>($"{GenresEndpoint}/{id}");
        Assert.NotNull(created);
        return created;
    }
}