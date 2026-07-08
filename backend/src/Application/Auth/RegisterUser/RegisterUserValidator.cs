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

        // Password strength (length + complexity) is owned entirely by ASP.NET Core Identity. Here
        // we only guard the input shape: non-empty, and bounded so an oversized value never reaches
        // the password hasher.
        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(256);
    }
}
