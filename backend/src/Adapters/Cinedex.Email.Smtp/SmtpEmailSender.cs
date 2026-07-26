using System.Net.Sockets;
using Cinedex.Application.Abstractions;
using Cinedex.Application.Email;
using Cinedex.Application.Exceptions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Cinedex.Email.Smtp;

internal sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mimeMessage = BuildMimeMessage(message);

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.SecureSocketOptions,
                cancellationToken);

            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception exception) when (
            exception is MailKit.Security.AuthenticationException or
                SmtpCommandException or
                SmtpProtocolException or
                SslHandshakeException or
                IOException or
                SocketException)
        {
            throw new EmailDeliveryException("The email could not be delivered by the SMTP server.", exception);
        }

        logger.LogInformation(
            "Sent email \"{Subject}\" to {Recipient} with tags {Tags}.",
            message.Subject,
            message.To.Address,
            message.Tags);
    }

    private MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage
        {
            Subject = message.Subject,
        };

        mimeMessage.From.Add(new MailboxAddress(
            _options.FromName ?? string.Empty,
            _options.FromAddress));
        mimeMessage.To.Add(new MailboxAddress(
            message.To.DisplayName ?? string.Empty,
            message.To.Address));

        var bodyBuilder = new BodyBuilder();
        switch (message.Body)
        {
            case HtmlEmailBody htmlBody:
                bodyBuilder.HtmlBody = htmlBody.Content;
                bodyBuilder.TextBody = htmlBody.PlainTextFallback;
                break;
            case PlainTextEmailBody plainTextBody:
                bodyBuilder.TextBody = plainTextBody.Content;
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported email body type '{message.Body.GetType().Name}'.");
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();
        return mimeMessage;
    }
}
