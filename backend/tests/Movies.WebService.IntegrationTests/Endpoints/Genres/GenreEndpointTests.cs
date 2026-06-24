using System.Net;
using System.Net.Http.Json;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;
using Movies.WebService.IntegrationTests.Constants;

namespace Movies.WebService.IntegrationTests.Genres;

public sealed class GenreEndpointTests(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
{
    [Fact]
    public async Task GetAllGenres_ReturnsSeededGenres()
    {
        var response = await fixture.Client.GetFromJsonAsync<GenresResponse>(TestRouteConstants.Genre.Endpoint);

        Assert.NotNull(response);
        var names = response.Genres.Select(genre => genre.Name).ToList();
        Assert.True(names.Count >= 17);
        Assert.Contains(TestGenreConstants.Action, names);
        Assert.Contains(TestGenreConstants.Western, names);
    }

    [Fact]
    public async Task CreateGenre_Returns201AndIsRetrievable()
    {
        var request = new CreateGenreRequest
        {
            Name = "Noir",
        };

        var createResponse = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Genre.Endpoint, request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        Assert.NotNull(createResponse.Headers.Location);
        var locationUri = createResponse.Headers.Location.ToString();
        Assert.StartsWith(TestRouteConstants.Genre.LocationPrefix, locationUri);

        var idString = locationUri.Substring(TestRouteConstants.Genre.LocationPrefix.Length);
        Assert.True(Guid.TryParse(idString, out var id));

        var fetched = await fixture.Client.GetFromJsonAsync<GenreResponse>($"{TestRouteConstants.Genre.Endpoint}/{id}");
        Assert.NotNull(fetched);
        Assert.Equal(request.Name, fetched.Name);
    }

    [Fact]
    public async Task CreateGenre_WithEmptyName_Returns400()
    {
        var request = new CreateGenreRequest { Name = string.Empty };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Genre.Endpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateGenre_ChangesName()
    {
        var created = await CreateGenreAsync("Heist");

        var update = new UpdateGenreRequest { Name = "Heist Thriller" };
        var updateResponse = await fixture.Client.PutAsJsonAsync($"{TestRouteConstants.Genre.Endpoint}/{created.Id}", update);
        Assert.Equal(HttpStatusCode.Accepted, updateResponse.StatusCode);

        var fetched = await fixture.Client.GetFromJsonAsync<GenreResponse>($"{TestRouteConstants.Genre.Endpoint}/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal("Heist Thriller", fetched.Name);
    }

    [Fact]
    public async Task DeleteGenre_RemovesGenre()
    {
        var created = await CreateGenreAsync("Disaster");

        var deleteResponse = await fixture.Client.DeleteAsync($"{TestRouteConstants.Genre.Endpoint}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await fixture.Client.GetAsync($"{TestRouteConstants.Genre.Endpoint}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetGenre_WithUnknownId_Returns404()
    {
        var response = await fixture.Client.GetAsync($"{TestRouteConstants.Genre.Endpoint}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<GenreResponse> CreateGenreAsync(string name)
    {
        var createResponse = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Genre.Endpoint, new CreateGenreRequest
        {
            Name = name,
        });
        Assert.NotNull(createResponse.Headers.Location);

        var locationUri = createResponse.Headers.Location.ToString();
        var idString = locationUri.Substring(TestRouteConstants.Genre.LocationPrefix.Length);
        var id = Guid.Parse(idString);

        var created = await fixture.Client.GetFromJsonAsync<GenreResponse>($"{TestRouteConstants.Genre.Endpoint}/{id}");
        Assert.NotNull(created);
        return created;
    }
}
