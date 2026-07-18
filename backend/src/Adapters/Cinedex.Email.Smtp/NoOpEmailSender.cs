using Cinedex.Application.Abstractions;
using Cinedex.Application.Email;
using Microsoft.Extensions.Logging;

namespace Cinedex.Email.Smtp;

/// <summary>
/// Placeholder <see cref="IEmailSender"/> that performs no delivery. Logs that a message was
/// requested, without emitting the body.
/// </summary>
/// <remarks>
/// The only registered <see cref="IEmailSender"/> until a real sender is wired up, so no email is
/// delivered end to end. To be replaced by a MailKit-based SmtpEmailSender.
/// </remarks>
internal sealed class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    /// <remarks>
    /// Logs the recipient and subject and returns <see cref="Task.CompletedTask"/>. No message is
    /// sent, and the body (which may contain secrets such as a reset link) is never logged.
    /// </remarks>
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        // TODO: Replace with a MailKit-based SmtpEmailSender. The built-in System.Net.Mail.SmtpClient
        // is obsolete and not recommended for new development; MailKit is the modern SMTP client and
        // can target any relay (self-hosted, Gmail, or a SendGrid/Mailgun/SES SMTP endpoint) via
        // config. Until then no email is delivered. Tracked in docs/auth-security-model.md.
        logger.LogInformation(
            "Email \"{Subject}\" to {Recipient} was not sent; email delivery is not configured.",
            message.Subject,
            message.To.Address);

        return Task.CompletedTask;
    }
}
