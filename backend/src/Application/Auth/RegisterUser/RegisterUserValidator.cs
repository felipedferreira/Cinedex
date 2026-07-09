using FluentValidation;

namespace Cinedex.Application.Auth.RegisterUser;

internal sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.UserName)
            .NotEmpty()
            .MaximumLength(256);

        // Password strength is owned by Identity (see PasswordPolicyConstants). Only the input
        // shape is guarded here, via the shared rule in PasswordRules.
        RuleFor(command => command.Password).PasswordInputGuard();
    }
}
