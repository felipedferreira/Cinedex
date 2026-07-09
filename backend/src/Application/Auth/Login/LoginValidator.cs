using FluentValidation;

namespace Cinedex.Application.Auth.Login;

internal sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        // Explicit messages: the login 400 body carries these strings to the client, so the wording
        // is a contract. Deliberately generic (no format hints, no "at least N characters") — login
        // must never leak information about what a valid credential looks like.
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email must not be empty.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Password must not be empty.");
    }
}
