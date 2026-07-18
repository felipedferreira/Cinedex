using Cinedex.Application.Validation;
using FluentValidation;

namespace Cinedex.Application.Auth.ForgotPassword;

internal sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailMustNotBeEmpty)
            .EmailAddress().WithMessage(ValidationMessages.EmailMustBeValid);
    }
}
