using System.Net;
using System.Text.Json;

namespace Movies.WebService.IntegrationTests;

public class WeatherForecastEndpointTests : IClassFixture<WebApplicationFixture>
{
    private readonly WebApplicationFixture fixture;

    public WeatherForecastEndpointTests(WebApplicationFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsOkStatus()
    {
        var response = await this.fixture.Client.GetAsync("/weatherforecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsJsonArray()
    {
        var response = await this.fixture.Client.GetAsync("/weatherforecast");
        var content = await response.Content.ReadAsStringAsync();

        using var jsonDoc = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Array, jsonDoc.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsFiveDayForecast()
    {
        var response = await this.fixture.Client.GetAsync("/weatherforecast");
        var content = await response.Content.ReadAsStringAsync();

        using var jsonDoc = JsonDocument.Parse(content);
        Assert.Equal(5, jsonDoc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetWeatherForecast_EachForecastHasRequiredFields()
    {
        var response = await this.fixture.Client.GetAsync("/weatherforecast");
        var content = await response.Content.ReadAsStringAsync();

        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        foreach (var element in root.EnumerateArray())
        {
            Assert.True(element.TryGetProperty("date", out _), "Missing 'date' property");
            Assert.True(element.TryGetProperty("temperatureC", out _), "Missing 'temperatureC' property");
            Assert.True(element.TryGetProperty("summary", out _), "Missing 'summary' property");
        }
    }
}