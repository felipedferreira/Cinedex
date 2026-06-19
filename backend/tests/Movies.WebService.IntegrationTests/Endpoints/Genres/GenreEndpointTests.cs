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
    public async Task CreateGenre_Returns201AndIsRetrievable()
    {
        var request = new CreateGenreRequest
        {
            Name = "Noir",
            Description = "Stylish crime dramas with cynical attitudes.",
        };

        var createResponse = await fixture.Client.PostAsJsonAsync(GenresEndpoint, request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(request.Name, created.Name);

        Assert.NotNull(createResponse.Headers.Location);
        Assert.EndsWith($"/genres/{created.Id}", createResponse.Headers.Location.ToString());

        var fetched = await fixture.Client.GetFromJsonAsync<GenreResponse>($"{GenresEndpoint}/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(request.Name, fetched.Name);
        Assert.Equal(request.Description, fetched.Description);
    }

    [Fact]
    public async Task CreateGenre_WithEmptyName_Returns400()
    {
        var request = new CreateGenreRequest { Name = string.Empty };

        var response = await fixture.Client.PostAsJsonAsync(GenresEndpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGenre_ChangesNameAndDescription()
    {
        var created = await CreateGenreAsync("Heist", "Caper films.");

        var update = new UpdateGenreRequest { Name = "Heist Thriller", Description = "Caper films with suspense." };
        var updateResponse = await fixture.Client.PutAsJsonAsync($"{GenresEndpoint}/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var fetched = await fixture.Client.GetFromJsonAsync<GenreResponse>($"{GenresEndpoint}/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal("Heist Thriller", fetched.Name);
        Assert.Equal("Caper films with suspense.", fetched.Description);
    }

    [Fact]
    public async Task DeleteGenre_RemovesGenre()
    {
        var created = await CreateGenreAsync("Disaster", "Catastrophe films.");

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

    private async Task<GenreResponse> CreateGenreAsync(string name, string description)
    {
        var response = await fixture.Client.PostAsJsonAsync(GenresEndpoint, new CreateGenreRequest
        {
            Name = name,
            Description = description,
        });
        var created = await response.Content.ReadFromJsonAsync<GenreResponse>();
        Assert.NotNull(created);
        return created;
    }
}
