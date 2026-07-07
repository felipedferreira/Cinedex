namespace Cinedex.Persistence.Auth.Identity.Options;

// Binds the "Jwt" configuration section. Shared by the adapter (token issuance) and the
// presentation layer (bearer token validation).
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}
