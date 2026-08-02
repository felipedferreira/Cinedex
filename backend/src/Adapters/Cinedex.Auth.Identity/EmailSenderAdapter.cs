using Cinedex.Application.Abstractions;
using Cinedex.Application.Email;
using Microsoft.AspNetCore.Identity;

namespace Cinedex.Auth.Identity;

// Bridges ASP.NET Core Identity's IEmailSender<TUser> (used by the Identity API/UI endpoints) onto
// the application's own IEmailSender port. Identity hands us a pre-built link or code and expects the
// sender to compose the message, so the minimal composition lives here rather than in the app layer.
internal sealed class EmailSenderAdapter : IEmailSender<ApplicationUser>
{
    private readonly IEmailSender _emailSender;

    public EmailSenderAdapter(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    /// <inheritdoc />
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        return SendAsync(
            email,
            "Confirm your email",
            new HtmlEmailBody(
                $"<p>Confirm your email address by <a href=\"{confirmationLink}\">clicking here</a>.</p>",
                PlainTextFallback: $"Confirm your email address: {confirmationLink}"),
            "email-confirmation");
    }

    /// <inheritdoc />
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        return SendAsync(
            email,
            "Reset your password",
            new HtmlEmailBody(
                $"<p>We received a request to reset your password. " +
                $"<a href=\"{resetLink}\">Reset it here</a>.</p>",
                PlainTextFallback: $"Reset your password: {resetLink}"),
            "password-reset");
    }

    /// <inheritdoc />
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        return SendAsync(
            email,
            "Your password reset code",
            new HtmlEmailBody(
                $"<p>Your password reset code is <strong>{resetCode}</strong>.</p>",
                PlainTextFallback: $"Your password reset code is {resetCode}."),
            "password-reset-code");
    }

    // Identity's interface exposes no CancellationToken, so none can be threaded through to the port.
    private Task SendAsync(string email, string subject, EmailBody body, string tag)
    {
        var message = new EmailMessage
        {
            To = new EmailRecipient(email),
            Subject = subject,
            Body = body,
            Tags = [tag],
        };

        return _emailSender.SendAsync(message, CancellationToken.None);
    }
}
