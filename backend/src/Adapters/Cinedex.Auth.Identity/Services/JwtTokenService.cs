using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cinedex.Application.Abstractions;
using Cinedex.Application.Auth;
using Cinedex.Application.Exceptions;
using Cinedex.Auth.Identity.Entities;
using Cinedex.Auth.Identity.Options;
using Cinedex.Domain.UserAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cinedex.Auth.Identity.Services;

internal sealed class JwtTokenService(
    AuthDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<AuthTokensDto> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var applicationUser = await userManager.FindByIdAsync(user.Id.ToString())
            ?? throw new InvalidCredentialsException("The user could not be found.");
        var roles = await userManager.GetRolesAsync(applicationUser);

        var rawRefreshToken = GenerateRefreshToken();

        // A login starts a new session, so it starts a new token family.
        var refreshEntity = CreateRefreshTokenEntity(user.Id, Guid.CreateVersion7(), rawRefreshToken);

        dbContext.RefreshTokens.Add(refreshEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateTokenResponse(user, roles, rawRefreshToken, refreshEntity.ExpiresAtUtc);
    }

    public async Task<AuthTokensDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);
        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existing = await dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existing is null || existing.ExpiresAtUtc <= now)
        {
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        var applicationUser = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == existing.UserId, cancellationToken);

        if (applicationUser is null)
        {
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        var domainUser = applicationUser.ToDomainUser();
        var roles = await userManager.GetRolesAsync(applicationUser);
        var rawRefreshToken = GenerateRefreshToken();

        // Rotation continues the incoming token's family rather than starting one. Reading it from
        // the non-tracked row above is safe even though a concurrent refresh may already have
        // revoked that row: FamilyId is init-only and never mutated, so no version of the row holds
        // a different value. RevokedAtUtc does change, which is why the update below re-checks it
        // instead of trusting this read — and why a loser of that race throws before persisting.
        var refreshEntity = CreateRefreshTokenEntity(domainUser.Id, existing.FamilyId, rawRefreshToken);

        // Rotate with a conditional update so concurrent refreshes cannot both win the same token.
        var revokedCount = await dbContext.RefreshTokens
            .Where(token =>
                token.TokenHash == tokenHash &&
                token.RevokedAtUtc == null &&
                token.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, now)
                    .SetProperty(token => token.ReplacedByTokenHash, refreshEntity.TokenHash),
                cancellationToken);

        if (revokedCount != 1)
        {
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        dbContext.RefreshTokens.Add(refreshEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateTokenResponse(domainUser, roles, rawRefreshToken, refreshEntity.ExpiresAtUtc);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);

        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existing is null || existing.RevokedAtUtc is not null)
        {
            return;
        }

        existing.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

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