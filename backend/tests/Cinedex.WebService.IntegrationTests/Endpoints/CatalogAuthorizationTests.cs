using System.Net;
using System.Net.Http.Json;
using Cinedex.WebService.Contracts.Enums;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.IntegrationTests.Constants;

namespace Cinedex.WebService.IntegrationTests;

// The catalog is members-only: every Title and Genre endpoint requires a bearer token.
// One representative read and one write pin that contract.
[Collection(WebApplicationCollection.Name)]
public sealed class CatalogAuthorizationTests(WebApplicationFixture fixture)
{
    [Fact]
    public async Task GetAllTitles_WithoutBearerToken_Returns401()
    {
        var response = await fixture.CookielessClient.GetAsync(TestRouteConstants.Title.Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTitle_WithoutBearerToken_Returns401()
    {
        var request = new CreateTitlesRequest
        {
            Title = "Anonymous Attempt",
            Type = TitleType.Movie,
            YearOfRelease = 2020,
        };

        var response = await fixture.CookielessClient.PostAsJsonAsync(TestRouteConstants.Title.Endpoint, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
