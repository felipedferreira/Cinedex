namespace Cinedex.WebService.IntegrationTests.Auth;

// Shares one Postgres container across the retention tests. A fixture rather than a per-test
// container because IAsyncLifetime runs per test instance, and starting Postgres once per test
// method would dominate the suite's runtime.
[CollectionDefinition(Name)]
public sealed class RefreshTokenCleanupCollection : ICollectionFixture<RefreshTokenCleanupFixture>
{
    public const string Name = "RefreshTokenCleanup";
}
