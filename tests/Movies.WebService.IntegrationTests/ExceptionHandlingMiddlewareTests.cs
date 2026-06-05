using System.Net;
using System.Text.Json;

namespace Movies.WebService.IntegrationTests;

public class ExceptionHandlingMiddlewareTests : IClassFixture<WebApplicationFixture>
{
    private readonly WebApplicationFixture fixture;

    public ExceptionHandlingMiddlewareTests(WebApplicationFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task TestException_ReturnsInternalServerError()
    {
        var response = await this.fixture.Client.GetAsync("/test-exception");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task TestException_ReturnsProblemDetailsJson()
    {
        var response = await this.fixture.Client.GetAsync("/test-exception");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var jsonDoc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Object, jsonDoc.RootElement.ValueKind);
    }

    [Fact]
    public async Task TestException_ReturnsProblemDetailsWithRequiredFields()
    {
        var response = await this.fixture.Client.GetAsync("/test-exception");
        var content = await response.Content.ReadAsStringAsync();

        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("type", out var typeProperty), "Missing 'type' property");
        Assert.True(root.TryGetProperty("title", out var titleProperty), "Missing 'title' property");
        Assert.True(root.TryGetProperty("status", out var statusProperty), "Missing 'status' property");
        Assert.True(root.TryGetProperty("detail", out var detailProperty), "Missing 'detail' property");
        Assert.True(root.TryGetProperty("instance", out var instanceProperty), "Missing 'instance' property");

        Assert.Equal("Internal Server Error", titleProperty.GetString());
        Assert.Equal(500, statusProperty.GetInt32());
        Assert.Equal("An unhandled exception occurred.", detailProperty.GetString());
        Assert.Equal("/test-exception", instanceProperty.GetString());
    }

    [Fact]
    public async Task TestException_IncludesTraceIdInResponse()
    {
        var response = await this.fixture.Client.GetAsync("/test-exception");
        var content = await response.Content.ReadAsStringAsync();

        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("extensions", out var extensionsProperty), "Missing 'extensions' property");
        Assert.True(extensionsProperty.TryGetProperty("traceId", out var traceIdProperty), "Missing 'traceId' in extensions");

        var traceId = traceIdProperty.GetString();
        Assert.NotNull(traceId);
        Assert.NotEmpty(traceId);
    }
}