using System.Net;
using System.Net.Http.Json;
using Movies.WebService.Contracts.Enums;
using Movies.WebService.Contracts.Requests;
using Movies.WebService.IntegrationTests.Constants;

namespace Movies.WebService.IntegrationTests.Titles;

public sealed class CreateTitleEndpointTests(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
{
    [Fact]
    public async Task CreateTitle_WithValidRequest_Returns201Created()
    {
        var request = new CreateTitlesRequest
        {
            Title = "Inception",
            Type = TitleType.Movie,
            YearOfRelease = 2010,
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTitle_WithValidRequest_ReturnsLocationHeader()
    {
        var request = new CreateTitlesRequest
        {
            Title = "Interstellar",
            Type = TitleType.Movie,
            YearOfRelease = 2014,
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);

        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith(TestRouteConstants.Title.LocationPrefix, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task CreateTitle_WithEmptyTitle_Returns400()
    {
        var request = new CreateTitlesRequest
        {
            Title = string.Empty,
            Type = TitleType.Movie,
            YearOfRelease = 2010,
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTitle_WithTitleExceedingMaxLength_Returns400()
    {
        var request = new CreateTitlesRequest
        {
            Title = new string('A', 257),
            Type = TitleType.Movie,
            YearOfRelease = 2010,
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTitle_WithYearBefore1888_Returns400()
    {
        var request = new CreateTitlesRequest
        {
            Title = "Too Old",
            Type = TitleType.Movie,
            YearOfRelease = 1887,
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTitle_WithYearTooFarInFuture_Returns400()
    {
        var request = new CreateTitlesRequest
        {
            Title = "Too Far Ahead",
            Type = TitleType.Movie,
            YearOfRelease = DateTime.UtcNow.Year + 6,
        };

        var response = await fixture.Client.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
