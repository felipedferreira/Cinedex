using Cinedex.Auth.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinedex.WebService.IntegrationTests.Auth;

public sealed class RefreshTokenCleanupOptionsValidationTests
{
    [Fact]
    public async Task StartAsync_WithValidCleanupConfiguration_Succeeds()
    {
        using var host = BuildHost(ValidConfiguration());

        await host.StartAsync(CancellationToken.None);
        await host.StopAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData("RefreshTokenCleanup:Interval", "00:00:00")]
    [InlineData("RefreshTokenCleanup:ExpiredRetention", "-1.00:00:00")]
    [InlineData("RefreshTokenCleanup:ReuseDetectionWindow", "00:00:00")]

    // Shorter than ExpiredRetention: revoked rows would be reaped before expired ones, silently
    // making the reuse-detection window the shorter of the two.
    [InlineData("RefreshTokenCleanup:ReuseDetectionWindow", "00:30:00")]
    [InlineData("RefreshTokenCleanup:BatchSize", "0")]
    [InlineData("RefreshTokenCleanup:BatchSize", "10001")]
    [InlineData("RefreshTokenCleanup:MaxBatchesPerRun", "0")]
    public async Task StartAsync_WithInvalidCleanupConfiguration_ThrowsOptionsValidationException(
        string key,
        string value)
    {
        var configuration = ValidConfiguration();
        configuration[key] = value;
        using var host = BuildHost(configuration);

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(CancellationToken.None));
    }

    // The host only needs the options graph to resolve. The sweep itself never runs: validation
    // fails before the worker starts, and in the valid case the interval outlives the test.
    private static IHost BuildHost(Dictionary<string, string?> configuration)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddAuthenticationPersistence();
        builder.Services.AddRefreshTokenCleanup();
        return builder.Build();
    }

    private static Dictionary<string, string?> ValidConfiguration() => new()
    {
        // Refused port with a one-second timeout. In the valid case the worker does start and
        // immediately attempts its startup sweep; the sweep's catch-all swallows the connection
        // failure, but Npgsql's 15-second default connect timeout would otherwise stall StopAsync.
        ["ConnectionStrings:DefaultConnection"] =
            "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=1;Command Timeout=1",
        ["RefreshTokenCleanup:Interval"] = "01:00:00",
        ["RefreshTokenCleanup:ExpiredRetention"] = "1.00:00:00",
        ["RefreshTokenCleanup:ReuseDetectionWindow"] = "14.00:00:00",
        ["RefreshTokenCleanup:BatchSize"] = "500",
        ["RefreshTokenCleanup:MaxBatchesPerRun"] = "20",
    };
}
