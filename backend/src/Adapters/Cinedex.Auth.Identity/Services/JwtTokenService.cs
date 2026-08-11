using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cinedex.Application.Abstractions;
using Cinedex.Application.Auth;
using Cinedex.Application.Exceptions;
using Cinedex.Auth.Identity.Options;
using Cinedex.Auth.Identity.Persistence.Repository;
using Cinedex.Domain.UserAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cinedex.Auth.Identity.Services;

internal sealed class JwtTokenService(
    IRefreshTokenRepository refreshTokens,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options,
    ILogger<JwtTokenService> logger) : ITokenService
{
    private static readonly EventId RefreshTokenReuseDetected = new(1001, "RefreshTokenReuseDetected");
    private readonly JwtOptions _options = options.Value;

    public async Task<AuthTokensDto> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var applicationUser = await userManager.FindByIdAsync(user.Id.ToString())
            ?? throw new InvalidCredentialsException("The user could not be found.");
        var roles = await userManager.GetRolesAsync(applicationUser);

        var rawRefreshToken = GenerateRefreshToken();

        // A login starts a new session, so it starts a new token family.
        var refreshEntity = CreateRefreshTokenEntity(user.Id, Guid.CreateVersion7(), rawRefreshToken);

        await refreshTokens.AddAsync(refreshEntity, cancellationToken);

        return CreateTokenResponse(user, roles, rawRefreshToken, refreshEntity.ExpiresAtUtc);
    }

    public async Task<AuthTokensDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);
        var now = DateTime.UtcNow;

        var preflight = await refreshTokens.FindByTokenHashAsync(tokenHash, cancellationToken);

        if (preflight is null || preflight.ExpiresAtUtc <= now)
        {
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        await using var transaction = await refreshTokens.BeginTransactionAsync(cancellationToken);
        await refreshTokens.AcquireFamilyLockAsync(preflight.FamilyId, cancellationToken);

        // The preflight read above avoids opening a transaction for unknown and expired tokens. A
        // family may have changed while this request waited for its advisory lock, so only this
        // second read is authoritative for rotation and reuse detection.
        var existing = await refreshTokens.FindByTokenHashAsync(tokenHash, cancellationToken);
        now = DateTime.UtcNow;

        if (existing is null || existing.ExpiresAtUtc <= now)
        {
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        if (existing.ReplacedByTokenHash is not null)
        {
            await RevokeActiveFamilyForReuseAsync(existing.FamilyId, now, transaction, cancellationToken);
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        if (existing.RevokedAtUtc is not null)
        {
            // Logout and an earlier family-wide response revoke without creating a replacement.
            // They are invalid attempts, but not new evidence that a rotated token was replayed.
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        // UserManager reads through the same scoped AuthDbContext the transaction was opened on, so
        // this stays inside it.
        var applicationUser = await userManager.FindByIdAsync(existing.UserId.ToString());

        if (applicationUser is null)
        {
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        var domainUser = applicationUser.ToDomainUser();
        var roles = await userManager.GetRolesAsync(applicationUser);
        var rawRefreshToken = GenerateRefreshToken();

        // Rotation continues the incoming token's family rather than starting one. FamilyId is
        // immutable for a row's lifetime, so the authoritative re-read under the family lock can be
        // copied safely. RevokedAtUtc can still change in a logout race, which is why the update
        // below remains conditional rather than trusting this read.
        var refreshEntity = CreateRefreshTokenEntity(domainUser.Id, existing.FamilyId, rawRefreshToken);

        // Rotate with a conditional update so concurrent refreshes cannot both win the same token.
        var revokedCount = await refreshTokens.RotateAsync(
            tokenHash,
            now,
            refreshEntity.TokenHash,
            cancellationToken);

        if (revokedCount != 1)
        {
            // The family lock serializes updated replicas, but keep the conditional update and this
            // re-read for logout races and rolling deployments where an older replica does not yet
            // participate in the advisory-lock protocol.
            var current = await refreshTokens.FindByTokenHashAsync(tokenHash, cancellationToken);
            var reuseDetectedAt = DateTime.UtcNow;

            if (current is not null &&
                current.ExpiresAtUtc > reuseDetectedAt &&
                current.ReplacedByTokenHash is not null)
            {
                await RevokeActiveFamilyForReuseAsync(
                    current.FamilyId,
                    reuseDetectedAt,
                    transaction,
                    cancellationToken);
            }

            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        await refreshTokens.AddAsync(refreshEntity, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateTokenResponse(domainUser, roles, rawRefreshToken, refreshEntity.ExpiresAtUtc);
    }

    // Idempotent: revoking an unknown or already-revoked token matches no row and is a no-op.
    public Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken) =>
        refreshTokens.RevokeByTokenHashAsync(HashToken(refreshToken), DateTime.UtcNow, cancellationToken);

    public async Task<Guid?> GetRefreshTokenUserIdAsync(string refreshToken, CancellationToken cancellationToken) =>
        (await refreshTokens.FindByTokenHashAsync(HashToken(refreshToken), cancellationToken))?.UserId;

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task RevokeActiveFamilyForReuseAsync(
        Guid familyId,
        DateTime detectedAtUtc,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var revokedTokenCount = await refreshTokens.RevokeActiveFamilyAsync(
            familyId,
            detectedAtUtc,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogWarning(
            RefreshTokenReuseDetected,
            "Refresh-token reuse detected; revoked {RevokedTokenCount} active token(s) in the compromised family.",
            revokedTokenCount);
    }

    // Builds the persistence-side entity, which stores only the hash of the raw token. The family is
    // supplied by the caller because that is the one respect in which the two call sites differ: a
    // login starts a new family, a rotation continues the incoming token's.
    private RefreshToken CreateRefreshTokenEntity(Guid userId, Guid familyId, string rawRefreshToken)
    {
        var now = DateTime.UtcNow;

        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresAtUtc = now.AddDays(_options.RefreshTokenDays),
            CreatedAtUtc = now,
        };
    }

    // Builds the client-facing response, which carries the raw token. The refresh expiry is taken
    // from the persisted entity so the two can never disagree on the token's lifetime.
    private AuthTokensDto CreateTokenResponse(User user, IList<string> roles, string rawRefreshToken, DateTime refreshTokenExpiresAtUtc)
    {
        var now = DateTime.UtcNow;
        var accessExpiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var accessToken = CreateAccessToken(user, roles, now, accessExpiresAt);

        return new AuthTokensDto(accessToken, accessExpiresAt, rawRefreshToken, refreshTokenExpiresAtUtc);
    }

    private string CreateAccessToken(User user, IList<string> roles, DateTime issuedAt, DateTime expiresAt)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
        };

        // One ClaimTypes.Role per assignment; the default JwtBearer RoleClaimType is ClaimTypes.Role,
        // so [Authorize(Roles = ...)] reads these directly.
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
