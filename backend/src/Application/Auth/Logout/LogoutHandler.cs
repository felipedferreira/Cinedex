using Cinedex.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Auth.Logout;

internal sealed class LogoutHandler(
    ITokenService tokenService,
    ILogger<LogoutHandler> logger) : ILogoutHandler
{
    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var tokenOwnerId = await tokenService.GetRefreshTokenUserIdAsync(
            command.RefreshToken,
            cancellationToken);

        if (tokenOwnerId is null)
        {
            logger.LogInformation("Refresh-token revocation skipped because the token is unknown.");
            return;
        }

        if (tokenOwnerId != command.RequestingUserId)
        {
            logger.LogInformation("Refresh-token revocation skipped because the token belongs to another user.");
            return;
        }

        await tokenService.RevokeRefreshTokenAsync(command.RefreshToken, cancellationToken);
    }
}
