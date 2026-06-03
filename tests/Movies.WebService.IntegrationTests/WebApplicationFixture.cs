using Microsoft.AspNetCore.Mvc.Testing;

namespace Movies.WebService.IntegrationTests;

public class WebApplicationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Client = CreateClient();
        await Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        Client.Dispose();
        await base.DisposeAsync();
    }
}
