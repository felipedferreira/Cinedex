using Cinedex.Application.Validation;
using FluentValidation;

namespace Cinedex.Application.Auth.ResetPassword;

internal sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailMustNotBeEmpty)
            .EmailAddress().WithMessage(ValidationMessages.EmailMustBeValid);

        RuleFor(command => command.ResetToken)
            .NotEmpty().WithMessage(ValidationMessages.ResetTokenMustNotBeEmpty);

        // Password strength is owned by Identity (see PasswordPolicyConstants). Only the input
        // shape is guarded here, via the shared rule in PasswordRules.
        RuleFor(command => command.NewPassword).PasswordInputGuard(
            ValidationMessages.NewPasswordMustNotBeEmpty,
            ValidationMessages.NewPasswordMustNotExceedLength);
    }
}