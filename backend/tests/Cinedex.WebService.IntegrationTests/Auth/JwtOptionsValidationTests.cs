using Cinedex.Auth.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinedex.WebService.IntegrationTests.Auth;

public sealed class JwtOptionsValidationTests
{
    [Theory]
    [InlineData("5")]
    [InlineData("15")]
    public async Task StartAsync_WithBoundaryAccessTokenLifetime_Succeeds(string accessTokenMinutes)
    {
        using var host = BuildHost(accessTokenMinutes);

        await host.StartAsync(CancellationToken.None);
        await host.StopAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData("4")]
    [InlineData("16")]
    public async Task StartAsync_WithAccessTokenLifetimeOutsideAllowedRange_ThrowsOptionsValidationException(
        string accessTokenMinutes)
    {
        using var host = BuildHost(accessTokenMinutes);

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("7")]
    public async Task StartAsync_WithBoundaryRefreshTokenLifetime_Succeeds(string refreshTokenDays)
    {
        using var host = BuildHost(refreshTokenDays: refreshTokenDays);

        await host.StartAsync(CancellationToken.None);
        await host.StopAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("8")]
    public async Task StartAsync_WithRefreshTokenLifetimeOutsideAllowedRange_ThrowsOptionsValidationException(
        string refreshTokenDays)
    {
        using var host = BuildHost(refreshTokenDays: refreshTokenDays);

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(CancellationToken.None));
    }

    private static IHost BuildHost(string accessTokenMinutes = "15", string refreshTokenDays = "7")
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:AccessTokenMinutes"] = accessTokenMinutes,
            ["Jwt:RefreshTokenDays"] = refreshTokenDays,
        });
        builder.Services.AddAuthenticationAdapter();
        return builder.Build();
    }
}
