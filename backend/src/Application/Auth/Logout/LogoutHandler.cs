using Cinedex.Application.Abstractions;

namespace Cinedex.Application.Auth.Logout;

internal sealed class LogoutHandler(ITokenService tokenService) : ILogoutHandler
{
    public Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken) =>
        tokenService.RevokeRefreshTokenAsync(command.RefreshToken, cancellationToken);
}
