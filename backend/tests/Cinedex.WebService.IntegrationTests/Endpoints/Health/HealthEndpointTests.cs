using System.Net;
using Cinedex.WebService.IntegrationTests.Constants;

namespace Cinedex.WebService.IntegrationTests.Health;

[Collection(WebApplicationCollection.Name)]
public sealed class HealthEndpointTests(WebApplicationFixture fixture)
{
    [Fact]
    public async Task GetReady_WithDefaultConfiguration_ReturnsHealthy()
    {
        var response = await fixture.Client.GetAsync(TestRouteConstants.Health.ReadyEndpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
