using FluentValidation;

namespace Cinedex.Application.Auth.ResetPassword;

internal sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(command => command.ResetToken)
            .NotEmpty();

        // Password strength is owned by Identity (see PasswordPolicyConstants). Only the input
        // shape is guarded here, via the shared rule in PasswordRules.
        RuleFor(command => command.NewPassword).PasswordInputGuard();
    }
}
