using Cinedex.Application.Abstractions;
using FluentValidation;

namespace Cinedex.Application.Auth.ResetPassword;

internal sealed class ResetPasswordHandler(
    IIdentityService identityService,
    IValidator<ResetPasswordCommand> validator) : IResetPasswordHandler
{
    public async Task HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        await identityService.ResetPasswordAsync(
            command.Email,
            command.ResetToken,
            command.NewPassword,
            cancellationToken);
    }
}
