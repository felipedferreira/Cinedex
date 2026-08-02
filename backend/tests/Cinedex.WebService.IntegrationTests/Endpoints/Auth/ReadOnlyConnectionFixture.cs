using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Cinedex.WebService.IntegrationTests.Auth;

/// <summary>
/// A web host wired exactly like <see cref="WebApplicationFixture"/>, except that
/// <c>ConnectionStrings:ReadOnlyConnection</c> points somewhere nothing is listening.
/// </summary>
/// <remarks>
/// Breaking the read connection on purpose is what makes the split observable from outside the
/// process: any request path that still works is provably running on the read-write connection, and
/// any path that fails is provably running on the read-only one. Asserting the wiring any other way
/// would mean reaching into internals the test project cannot see.
/// </remarks>
public sealed class ReadOnlyConnectionFixture : WebApplicationFixture
{
    // Port 1 on loopback, with the shortest timeouts Npgsql accepts so the failure is immediate
    // rather than a test that hangs. Same shape as the refused endpoint
    // RefreshTokenCleanupOptionsValidationTests uses.
    private const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=movies;Username=nobody;Password=nobody;Timeout=1;Command Timeout=1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Added after the base fixture's sources, so this wins for the one key it sets and leaves
        // DefaultConnection pointing at the real Testcontainer.
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReadOnlyConnection"] = UnreachableConnectionString,
            }));
    }
}
