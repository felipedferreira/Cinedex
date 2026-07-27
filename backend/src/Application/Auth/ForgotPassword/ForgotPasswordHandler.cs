using Cinedex.Application.Abstractions;
using Cinedex.Application.Configuration;
using Cinedex.Application.Email;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Auth.ForgotPassword;

internal sealed class ForgotPasswordHandler(
    IIdentityService identityService,
    IEmailDispatcher emailDispatcher,
    FrontendOptions frontendOptions,
    IValidator<ForgotPasswordCommand> validator,
    ILogger<ForgotPasswordHandler> logger) : IForgotPasswordHandler
{
    public async Task HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var resetToken = await identityService.GeneratePasswordResetTokenAsync(command.Email, cancellationToken);

        // Respond identically whether or not the account exists, to avoid account enumeration. That
        // covers latency as well as the status code: the reset email is queued rather than sent, so
        // the known-account path no longer waits on an SMTP conversation the unknown path skips.
        if (resetToken is null)
        {
            logger.LogInformation("Password reset requested for unknown email; ignoring.");
            return;
        }

        // Enqueue is non-blocking and cannot throw, so neither delivery latency nor delivery failure
        // can reach the response. EmailDeliveryWorker logs failures instead.
        emailDispatcher.Enqueue(BuildResetEmail(command.Email, resetToken));
    }

    // Composes the password-reset email here, in the application layer, so the transport adapter
    // stays a dumb pipe: the reset link is built from the token and the configured SPA URL, and the
    // branded shell comes from CinedexEmailLayout.
    private EmailMessage BuildResetEmail(string email, string resetToken)
    {
        var resetLink =
            $"{frontendOptions.BaseUrl}/reset-password" +
            $"?email={Uri.EscapeDataString(email)}" +
            $"&token={Uri.EscapeDataString(resetToken)}";

        const string introHtml = "We received a request to reset the password for your Cinedex account. "
            + "Choose a new one below.";

        var plainTextBody = $"""
            Reset your password

            We received a request to reset the password for your Cinedex account.
            Open this link to choose a new one:

            {resetLink}

            This link expires in 1 hour.

            Didn't request this? Ignore this email - your password won't change.
            """;

        var body = CinedexEmailLayout.Render(new EmailLayoutContent(
            Heading: "Reset your password",
            IntroHtml: introHtml,
            ButtonLabel: "Reset password",
            ButtonUrl: resetLink,
            FootnoteHtml: "This link expires in 1 hour.",
            PlainTextBody: plainTextBody));

        return new EmailMessage
        {
            To = new EmailRecipient(email),
            Subject = "Reset your password",
            Body = body,
            Tags = ["password-reset"],
        };
    }
}
