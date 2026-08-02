using Microsoft.EntityFrameworkCore;

namespace Cinedex.Auth.Identity.Persistence.Query;

/// <inheritdoc />
internal sealed class RefreshTokenQueries(AuthReadOnlyDbContext dbContext) : IRefreshTokenQueries
{
    /// <inheritdoc />
    public Task<RefreshTokenReadModel?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => new RefreshTokenReadModel(
                token.Id,
                token.UserId,
                token.FamilyId,
                token.ExpiresAtUtc,
                token.RevokedAtUtc,
                token.ReplacedByTokenHash))
            .FirstOrDefaultAsync(cancellationToken);
}
