using Cinedex.Application.Abstractions;
using Cinedex.Application.Configuration;
using Cinedex.Application.Email;
using Cinedex.Application.Exceptions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Auth.ForgotPassword;

internal sealed class ForgotPasswordHandler(
    IIdentityService identityService,
    IEmailSender emailSender,
    FrontendOptions frontendOptions,
    IValidator<ForgotPasswordCommand> validator,
    ILogger<ForgotPasswordHandler> logger) : IForgotPasswordHandler
{
    public async Task HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var resetToken = await identityService.GeneratePasswordResetTokenAsync(command.Email, cancellationToken);

        // Respond identically whether or not the account exists to avoid account enumeration.
        if (resetToken is null)
        {
            logger.LogInformation("Password reset requested for unknown email; ignoring.");
            return;
        }

        try
        {
            await emailSender.SendAsync(BuildResetEmail(command.Email, resetToken), cancellationToken);
        }
        catch (EmailDeliveryException exception)
        {
            // Keep the response identical for known and unknown accounts, even during an email
            // provider outage. The body and address are deliberately excluded from the log.
            logger.LogError(exception, "Password reset email delivery failed.");
        }
    }

    // Composes the password-reset email here, in the application layer, so the transport adapter
    // stays a dumb pipe: the reset link and copy are built from the token and the configured SPA URL.
    private EmailMessage BuildResetEmail(string email, string resetToken)
    {
        var resetLink =
            $"{frontendOptions.BaseUrl}/reset-password" +
            $"?email={Uri.EscapeDataString(email)}" +
            $"&token={Uri.EscapeDataString(resetToken)}";

        return new EmailMessage
        {
            To = new EmailRecipient(email),
            Subject = "Reset your password",
            Body = new HtmlEmailBody(
                $"<p>We received a request to reset your password. " +
                $"<a href=\"{resetLink}\">Reset it here</a>.</p>",
                PlainTextFallback: $"Reset your password: {resetLink}"),
            Tags = ["password-reset"],
        };
    }
}
