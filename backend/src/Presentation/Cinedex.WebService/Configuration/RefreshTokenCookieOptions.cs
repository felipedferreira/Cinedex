namespace Cinedex.WebService.Configuration;

/// <summary>
/// Controls the deployment-specific secure attribute of the refresh-token cookie.
/// </summary>
public sealed class RefreshTokenCookieOptions
{
    /// <summary>The configuration section that supplies these options.</summary>
    public const string SectionName = "Authentication:RefreshTokenCookie";

    /// <summary>
    /// Gets or sets a value indicating whether the browser must use HTTPS when sending the
    /// refresh-token cookie.
    /// </summary>
    public bool Secure { get; set; } = true;
}
