using System.Net.Http.Json;
using Cinedex.Application.Abstractions;
using Cinedex.Application.Email;
using Cinedex.Email.Smtp;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cinedex.WebService.IntegrationTests.Email;

public sealed class SmtpEmailSenderTests : IAsyncLifetime
{
    private const ushort MailpitHttpPort = 8025;
    private const ushort MailpitSmtpPort = 1025;
    private const string SmtpUsername = "cinedex-tests";
    private const string SmtpPassword = "cinedex-tests-password";
    private static readonly byte[] PngHeader = [137, 80, 78, 71, 13, 10, 26, 10];
    private readonly IContainer _mailpitContainer = new ContainerBuilder("axllent/mailpit:v1.30.0")
        .WithEnvironment("MP_SMTP_AUTH", $"{SmtpUsername}:{SmtpPassword}")
        .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "true")
        .WithPortBinding(MailpitHttpPort, true)
        .WithPortBinding(MailpitSmtpPort, true)
        .WithWaitStrategy(
            Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(MailpitHttpPort)
                .UntilInternalTcpPortIsAvailable(MailpitSmtpPort))
        .Build();

    public async ValueTask InitializeAsync() => await _mailpitContainer.StartAsync();

    public async ValueTask DisposeAsync() => await _mailpitContainer.DisposeAsync();

    [Fact]
    public async Task SendAsync_WithSupportedBodies_DeliversAuthenticatedMessagesToSmtpServer()
    {
        using var serviceProvider = BuildServiceProvider();
        var sender = serviceProvider.GetRequiredService<IEmailSender>();
        var suffix = Guid.NewGuid().ToString("N");
        var recipient = new EmailRecipient($"recipient-{suffix}@example.com", "Cinedex Recipient");
        var htmlSubject = $"HTML message {suffix}";
        var plainTextSubject = $"Plain-text message {suffix}";

        await sender.SendAsync(
            new EmailMessage
            {
                To = recipient,
                Subject = htmlSubject,
                Body = new HtmlEmailBody(
                    "<p>HTML delivery test</p>",
                    PlainTextFallback: "HTML delivery test"),
                Tags = ["smtp-integration-test"],
            },
            CancellationToken.None);

        await sender.SendAsync(
            new EmailMessage
            {
                To = recipient,
                Subject = plainTextSubject,
                Body = new PlainTextEmailBody("Plain-text delivery test"),
                Tags = ["smtp-integration-test"],
            },
            CancellationToken.None);

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(
                $"http://{_mailpitContainer.Hostname}:{_mailpitContainer.GetMappedPublicPort(MailpitHttpPort)}"),
        };

        var htmlMessage = await GetMessageAsync(httpClient, htmlSubject);
        Assert.Equal("no-reply@cinedex.test", htmlMessage.From.Address);
        Assert.Equal("Cinedex Tests", htmlMessage.From.Name);
        Assert.Equal(SmtpUsername, htmlMessage.Username);
        Assert.Equal(recipient.Address, Assert.Single(htmlMessage.To).Address);
        Assert.Equal(recipient.DisplayName, Assert.Single(htmlMessage.To).Name);
        Assert.Equal("HTML delivery test", htmlMessage.Text);
        Assert.Equal("<p>HTML delivery test</p>", htmlMessage.Html);

        var plainTextMessage = await GetMessageAsync(httpClient, plainTextSubject);
        Assert.Equal(SmtpUsername, plainTextMessage.Username);
        Assert.Equal("Plain-text delivery test", plainTextMessage.Text.TrimEnd('\r', '\n'));
        Assert.True(string.IsNullOrEmpty(plainTextMessage.Html));
    }

    [Fact]
    public async Task SendAsync_WithInlineImage_DeliversItAsALinkedResource()
    {
        using var serviceProvider = BuildServiceProvider();
        var sender = serviceProvider.GetRequiredService<IEmailSender>();
        var suffix = Guid.NewGuid().ToString("N");
        var subject = $"Inline image message {suffix}";

        await sender.SendAsync(
            new EmailMessage
            {
                To = new EmailRecipient($"recipient-{suffix}@example.com", "Cinedex Recipient"),
                Subject = subject,
                Body = new HtmlEmailBody(
                    "<p><img src=\"cid:cinedex-logo\" alt=\"Cinedex\" /></p>",
                    PlainTextFallback: "Cinedex")
                {
                    InlineImages = [new InlineImage("cinedex-logo", "image/png", PngHeader)],
                },
                Tags = ["smtp-integration-test"],
            },
            CancellationToken.None);

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(
                $"http://{_mailpitContainer.Hostname}:{_mailpitContainer.GetMappedPublicPort(MailpitHttpPort)}"),
        };

        var message = await GetMessageAsync(httpClient, subject);

        var inline = Assert.Single(message.Inline);
        Assert.Equal("cinedex-logo", inline.ContentID);
        Assert.Equal("image/png", inline.ContentType);
    }

    private static async Task<MailpitMessage> GetMessageAsync(HttpClient httpClient, string subject)
    {
        // 50 x 100 ms = 5 s. Generous for a loaded CI runner; the loop returns as soon as the
        // message appears, so the ceiling costs nothing on the happy path.
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var messages = await httpClient.GetFromJsonAsync<MailpitMessageList>("api/v1/messages");
            var summary = messages?.Messages.SingleOrDefault(message => message.Subject == subject);
            if (summary is not null)
            {
                return await httpClient.GetFromJsonAsync<MailpitMessage>($"api/v1/message/{summary.Id}")
                    ?? throw new InvalidOperationException("Mailpit returned an empty message.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException($"Mailpit did not capture the message with subject '{subject}'.");
    }

    private ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:Host"] = _mailpitContainer.Hostname,
                ["Smtp:Port"] = _mailpitContainer.GetMappedPublicPort(MailpitSmtpPort).ToString(),
                ["Smtp:Username"] = SmtpUsername,
                ["Smtp:Password"] = SmtpPassword,
                ["Smtp:FromAddress"] = "no-reply@cinedex.test",
                ["Smtp:FromName"] = "Cinedex Tests",
                ["Smtp:SecureSocketOptions"] = "None",
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddEmailAdapter()
            .BuildServiceProvider();
    }

    private sealed record MailpitMessageList(IReadOnlyList<MailpitMessageSummary> Messages);

    private sealed record MailpitMessageSummary(string Id, string Subject);

    private sealed record MailpitMessage(
        MailpitAddress From,
        IReadOnlyList<MailpitAddress> To,
        string Subject,
        string Text,
        string Html,
        string Username,
        IReadOnlyList<MailpitInlinePart> Inline);

    private sealed record MailpitAddress(string Address, string Name);

    private sealed record MailpitInlinePart(string ContentID, string ContentType, string FileName);
}
