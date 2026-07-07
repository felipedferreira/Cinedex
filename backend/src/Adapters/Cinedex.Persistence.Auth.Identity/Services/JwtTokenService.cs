using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cinedex.Application.Abstractions;
using Cinedex.Application.Auth;
using Cinedex.Application.Exceptions;
using Cinedex.Domain.UserAggregate;
using Cinedex.Persistence.Auth.Identity.Entities;
using Cinedex.Persistence.Auth.Identity.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cinedex.Persistence.Auth.Identity.Services;

internal sealed class JwtTokenService(AuthDbContext dbContext, IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<AuthTokensDto> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (dto, refreshEntity) = CreateTokens(user);

        dbContext.RefreshTokens.Add(refreshEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return dto;
    }

    public async Task<AuthTokensDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);

        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existing is null || existing.RevokedAtUtc is not null || existing.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        var applicationUser = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == existing.UserId, cancellationToken);

        if (applicationUser is null)
        {
            throw new InvalidCredentialsException("The refresh token is invalid or has expired.");
        }

        var (dto, refreshEntity) = CreateTokens(applicationUser.ToDomainUser());

        // Rotate: revoke the presented token and persist its replacement atomically.
        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.ReplacedByTokenHash = refreshEntity.TokenHash;
        dbContext.RefreshTokens.Add(refreshEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return dto;
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

    private (AuthTokensDto Dto, RefreshToken RefreshEntity) CreateTokens(User user)
    {
        var now = DateTime.UtcNow;
        var accessExpiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshExpiresAt = now.AddDays(_options.RefreshTokenDays);

        var accessToken = CreateAccessToken(user, now, accessExpiresAt);
        var rawRefreshToken = GenerateRefreshToken();

        var refreshEntity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresAtUtc = refreshExpiresAt,
            CreatedAtUtc = now,
        };

        var dto = new AuthTokensDto(accessToken, accessExpiresAt, rawRefreshToken, refreshExpiresAt);
        return (dto, refreshEntity);
    }

    private string CreateAccessToken(User user, DateTime issuedAt, DateTime expiresAt)
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
