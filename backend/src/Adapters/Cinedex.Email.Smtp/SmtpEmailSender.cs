using Cinedex.Application.Abstractions;
using Cinedex.Application.Email;
using Cinedex.Application.Exceptions;
using MailKit.Net.Smtp;
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
        using var client = new SmtpClient();

        try
        {
            // Built inside the try so that a MimeKit failure — ContentType.Parse on an inline
            // image's media type, say — surfaces as the EmailDeliveryException the port documents
            // rather than as a raw MimeKit exception.
            var mimeMessage = BuildMimeMessage(message);

            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.SecureSocketOptions,
                cancellationToken);

            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new EmailDeliveryException("The email could not be delivered by the SMTP server.", exception);
        }

        // The recipient address is deliberately excluded. A password-reset send logged against an
        // address turns the log store into a record of who requested a reset — the same enumeration
        // signal the forgot-password endpoint suppresses. Subject and tags are enough to correlate.
        logger.LogInformation(
            "Sent email \"{Subject}\" with tags {Tags}.",
            message.Subject,
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

                foreach (var image in htmlBody.InlineImages)
                {
                    var resource = bodyBuilder.LinkedResources.Add(
                        image.ContentId,
                        image.Content.ToArray(),
                        ContentType.Parse(image.MediaType));
                    resource.ContentId = image.ContentId;
                }

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
