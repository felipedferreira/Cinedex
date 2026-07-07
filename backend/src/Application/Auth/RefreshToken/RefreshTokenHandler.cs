using Cinedex.Application.Abstractions;
using FluentValidation;

namespace Cinedex.Application.Auth.RefreshToken;

internal sealed class RefreshTokenHandler(
    ITokenService tokenService,
    IValidator<RefreshTokenCommand> validator) : IRefreshTokenHandler
{
    public async Task<AuthTokensDto> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        return await tokenService.RefreshAsync(command.RefreshToken, cancellationToken);
    }
}
