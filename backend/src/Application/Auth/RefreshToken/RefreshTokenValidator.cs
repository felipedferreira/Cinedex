using Cinedex.Application.Validation;
using FluentValidation;

namespace Cinedex.Application.Auth.RefreshToken;

internal sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage(ValidationMessages.RefreshTokenMustNotBeEmpty);
    }
}