using Cinedex.Application.Validation;
using FluentValidation;

namespace Cinedex.Application.Auth.RegisterUser;

internal sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailMustNotBeEmpty)
            .EmailAddress().WithMessage(ValidationMessages.EmailMustBeValid)
            .MaximumLength(256).WithMessage(ValidationMessages.EmailMustNotExceedLength);

        RuleFor(command => command.UserName)
            .NotEmpty().WithMessage(ValidationMessages.UsernameMustNotBeEmpty)
            .MaximumLength(256).WithMessage(ValidationMessages.UsernameMustNotExceedLength);

        // Password strength is owned by Identity (see PasswordPolicyConstants). Only the input
        // shape is guarded here, via the shared rule in PasswordRules.
        RuleFor(command => command.Password).PasswordInputGuard(
            ValidationMessages.PasswordMustNotBeEmpty,
            ValidationMessages.PasswordMustNotExceedLength);
    }
}