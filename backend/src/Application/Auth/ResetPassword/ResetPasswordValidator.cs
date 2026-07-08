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

        // Password strength (length + complexity) is owned entirely by ASP.NET Core Identity. Here
        // we only guard the input shape: non-empty, and bounded so an oversized value never reaches
        // the password hasher.
        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MaximumLength(256);
    }
}
