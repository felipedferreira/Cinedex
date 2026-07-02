namespace Cinedex.WebService.IntegrationTests;

// Shares a single app host + Postgres container across all endpoint test classes.
// Booting multiple WebApplicationFactory<Program> hosts in parallel races on
// shared JsonSerializerOptions state inside FastEndpoints/System.Text.Json.
[CollectionDefinition(Name)]
public sealed class WebApplicationCollection : ICollectionFixture<WebApplicationFixture>
{
    public const string Name = "WebApplication";
}
