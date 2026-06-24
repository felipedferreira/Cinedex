using System.Net;
using System.Net.Http.Json;
using Movies.WebService.Contracts.Enums;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.Contracts.Responses;
using Movies.WebService.IntegrationTests.Constants;

namespace Movies.WebService.IntegrationTests.Titles;

public sealed class TitleGenreEndpointTests(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
{
    [Fact]
    public async Task CreateTitle_WithDescription_PersistsDescription()
    {
        var request = new CreateTitlesRequest
        {
            Title = "Blade Runner 2049",
            Type = TitleType.Movie,
            YearOfRelease = 2017,
            Description = "A young blade runner discovers a long-buried secret.",
        };

        var createResponse = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);
        Assert.NotNull(createResponse.Headers.Location);

        var locationUri = createResponse.Headers.Location.ToString();
        var idString = locationUri.Substring(TestRouteConstants.Title.LocationPrefix.Length);
        var id = Guid.Parse(idString);

        var fetched = await fixture.Client.GetFromJsonAsync<TitleResponse>($"{TestRouteConstants.Title.Endpoint}/{id}");

        Assert.NotNull(fetched);
        Assert.Equal(request.Description, fetched.Description);
    }

    [Fact]
    public async Task CreateTitle_WithGenreIds_LinksGenres()
    {
        var genres = await GetSeededGenresAsync();
        var sciFi = genres.Single(genre => genre.Name == TestGenreConstants.SciFi);
        var thriller = genres.Single(genre => genre.Name == TestGenreConstants.Thriller);

        var request = new CreateTitlesRequest
        {
            Title = "Minority Report",
            Type = TitleType.Movie,
            YearOfRelease = 2002,
            GenreIds = [sciFi.Id, thriller.Id],
        };

        var createResponse = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);
        Assert.NotNull(createResponse.Headers.Location);

        var locationUri = createResponse.Headers.Location.ToString();
        var idString = locationUri.Substring(TestRouteConstants.Title.LocationPrefix.Length);
        var id = Guid.Parse(idString);

        var fetched = await fixture.Client.GetFromJsonAsync<TitleDetailsResponse>($"{TestRouteConstants.Title.Endpoint}/{id}");

        Assert.NotNull(fetched);
        Assert.Equal(2, fetched.Genres.Count());
        Assert.Contains(fetched.Genres, genre => genre.Id == sciFi.Id && genre.Name == TestGenreConstants.SciFi);
        Assert.Contains(fetched.Genres, genre => genre.Id == thriller.Id);
    }

    [Fact]
    public async Task UpdateTitle_ReplacesLinkedGenres()
    {
        var genres = await GetSeededGenresAsync();
        var action = genres.Single(genre => genre.Name == TestGenreConstants.Action);
        var drama = genres.Single(genre => genre.Name == TestGenreConstants.Drama);
        var crime = genres.Single(genre => genre.Name == TestGenreConstants.Crime);

        var createResponse = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, new CreateTitlesRequest
        {
            Title = "Heat",
            Type = TitleType.Movie,
            YearOfRelease = 1995,
            GenreIds = [action.Id, drama.Id],
        });
        Assert.NotNull(createResponse.Headers.Location);

        var locationUri = createResponse.Headers.Location.ToString();
        var idString = locationUri.Substring(TestRouteConstants.Title.LocationPrefix.Length);
        var id = Guid.Parse(idString);

        var updateResponse = await fixture.Client.PutAsJsonAsync($"{TestRouteConstants.Title.Endpoint}/{id}", new UpdateTitlesRequest
        {
            Title = "Heat",
            Type = TitleType.Movie,
            YearOfRelease = 1995,
            GenreIds = [crime.Id],
        });
        Assert.Equal(HttpStatusCode.Accepted, updateResponse.StatusCode);

        var fetched = await fixture.Client.GetFromJsonAsync<TitleDetailsResponse>($"{TestRouteConstants.Title.Endpoint}/{id}");

        Assert.NotNull(fetched);
        var genre = Assert.Single(fetched.Genres);
        Assert.Equal(crime.Id, genre.Id);
    }

    [Fact]
    public async Task CreateTitle_WithUnknownGenreId_Returns404()
    {
        var request = new CreateTitlesRequest
        {
            Title = "Ghost Genre",
            Type = TitleType.Movie,
            YearOfRelease = 2020,
            GenreIds = [Guid.NewGuid()],
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<IReadOnlyList<GenreResponse>> GetSeededGenresAsync()
    {
        var response = await fixture.Client.GetFromJsonAsync<GenresResponse>(TestRouteConstants.Genre.Endpoint);
        Assert.NotNull(response);
        return response.Genres.ToList();
    }
}
