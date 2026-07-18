namespace Cinedex.Application.Auth.RefreshToken;

public interface IRefreshTokenHandler
{
    Task<AuthTokensDto> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken);
}
