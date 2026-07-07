using Cinedex.Application.Abstractions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Auth.ForgotPassword;

internal sealed class ForgotPasswordHandler(
    IIdentityService identityService,
    IEmailSender emailSender,
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

        await emailSender.SendPasswordResetAsync(command.Email, resetToken, cancellationToken);
    }
}
