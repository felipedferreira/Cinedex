using Cinedex.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cinedex.Email.Smtp;

/// <summary>
/// Placeholder <see cref="IEmailSender"/> that performs no delivery. Logs that a reset was
/// requested, without emitting the token value.
/// </summary>
/// <remarks>
/// The only registered <see cref="IEmailSender"/> until a real sender is wired up, so password reset
/// is not usable end to end. To be replaced by a MailKit-based SmtpEmailSender.
/// </remarks>
internal sealed class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    /// <remarks>
    /// Logs the request and returns <see cref="Task.CompletedTask"/>. No message is sent, and
    /// <paramref name="resetToken"/> is deliberately never written to the log.
    /// </remarks>
    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken)
    {
        // TODO: Replace with a MailKit-based SmtpEmailSender. The built-in System.Net.Mail.SmtpClient
        // is obsolete and not recommended for new development; MailKit is the modern SMTP client and
        // can target any relay (self-hosted, Gmail, or a SendGrid/Mailgun/SES SMTP endpoint) via
        // config. Until then the reset token never reaches the user and POST /auth/password/reset can
        // only be exercised with a token obtained out of band. Tracked in docs/auth-security-model.md.
        logger.LogInformation(
            "Password reset requested for {Email}. Email delivery is not configured; token was not sent.",
            email);

        return Task.CompletedTask;
    }
}
