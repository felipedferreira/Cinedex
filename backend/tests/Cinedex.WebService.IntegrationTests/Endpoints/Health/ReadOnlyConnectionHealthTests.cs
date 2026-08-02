using System.Net;
using Cinedex.WebService.IntegrationTests.Auth;
using Cinedex.WebService.IntegrationTests.Constants;

namespace Cinedex.WebService.IntegrationTests.Health;

/// <summary>
/// Proves that <c>/health/ready</c> reflects the read-only connection, not just the default one, by
/// running a host whose <c>ConnectionStrings:ReadOnlyConnection</c> points nowhere.
/// </summary>
public sealed class ReadOnlyConnectionHealthTests(ReadOnlyConnectionFixture fixture)
    : IClassFixture<ReadOnlyConnectionFixture>
{
    [Fact]
    public async Task GetReady_WithReadOnlyConnectionUnreachable_ReturnsUnhealthy()
    {
        var response = await fixture.Client.GetAsync(TestRouteConstants.Health.ReadyEndpoint);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
