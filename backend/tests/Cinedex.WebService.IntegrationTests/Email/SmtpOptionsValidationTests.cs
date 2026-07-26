using Cinedex.Email.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinedex.WebService.IntegrationTests.Email;

public sealed class SmtpOptionsValidationTests
{
    [Fact]
    public async Task StartAsync_WithValidSmtpConfiguration_Succeeds()
    {
        using var host = BuildHost(ValidConfiguration());

        await host.StartAsync(CancellationToken.None);
        await host.StopAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData("Smtp:Host", "")]
    [InlineData("Smtp:Port", "0")]
    [InlineData("Smtp:FromAddress", "not-an-email-address")]
    [InlineData("Smtp:Username", "")]
    [InlineData("Smtp:Password", "")]
    [InlineData("Smtp:SecureSocketOptions", "999")]
    public async Task StartAsync_WithInvalidSmtpConfiguration_ThrowsOptionsValidationException(
        string key,
        string value)
    {
        var configuration = ValidConfiguration();
        configuration[key] = value;
        using var host = BuildHost(configuration);

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(CancellationToken.None));
    }

    private static IHost BuildHost(Dictionary<string, string?> configuration)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddEmailAdapter();
        return builder.Build();
    }

    private static Dictionary<string, string?> ValidConfiguration() => new()
    {
        ["Smtp:Host"] = "localhost",
        ["Smtp:Port"] = "1025",
        ["Smtp:Username"] = "cinedex-tests",
        ["Smtp:Password"] = "cinedex-tests-password",
        ["Smtp:FromAddress"] = "no-reply@cinedex.test",
        ["Smtp:FromName"] = "Cinedex Tests",
        ["Smtp:SecureSocketOptions"] = "None",
    };
}
