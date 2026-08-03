namespace Cinedex.Persistence.Tests.Integration;

/// <summary>
/// Shares one container across the integration tests, and runs them sequentially.
/// </summary>
/// <remarks>
/// Sequential execution is not just an economy. Several of these tests assert on total row counts or
/// contend for the same advisory-lock key, and running them concurrently would make them interfere
/// with each other in ways that look like flakiness in the library rather than in the suite.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
