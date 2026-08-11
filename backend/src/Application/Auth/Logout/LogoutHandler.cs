using Cinedex.Application.Abstractions;

namespace Cinedex.Application.Auth.Logout;

internal sealed class LogoutHandler(ITokenService tokenService) : ILogoutHandler
{
    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var tokenOwnerId = await tokenService.GetRefreshTokenUserIdAsync(
            command.RefreshToken,
            cancellationToken);

        if (tokenOwnerId is null)
        {
            return;
        }

        if (tokenOwnerId != command.RequestingUserId)
        {
            return;
        }

        await tokenService.RevokeRefreshTokenAsync(command.RefreshToken, cancellationToken);
    }
}
