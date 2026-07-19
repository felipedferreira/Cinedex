using Cinedex.Application.Validation;
using FluentValidation;

namespace Cinedex.Application.Auth.Login;

internal sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        // The messages here surface in the 400 body under errors.email / errors.password. Login
        // deliberately does NOT check length or complexity — those would leak credential shape.
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailMustNotBeEmpty)
            .EmailAddress().WithMessage(ValidationMessages.EmailMustBeValid);

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage(ValidationMessages.PasswordMustNotBeEmpty);
    }
}