using Cinedex.WebService.Constants;

namespace Cinedex.WebService.Endpoints.Auth;

/// <summary>
/// Reads, writes and clears the refresh-token cookie. The raw refresh token never appears in a
/// response body, so a cross-site scripting defect cannot reach it.
/// </summary>
/// <remarks>
/// <para>
/// <c>SameSite=Strict</c> means the browser never attaches the cookie to a request originated by
/// another site, which is what makes CSRF against <c>/auth/refresh</c> impossible rather than merely
/// defended against. It also makes a same-site deployment a hard requirement: see
/// <c>docs/auth-security-model.md</c>.
/// </para>
/// <para>
/// A cookie is only deleted when the delete call's <c>Path</c>, <c>Domain</c>, <c>Secure</c> and
/// <c>SameSite</c> match the ones it was set with. <see cref="BuildOptions"/> is the single source of
/// those values so a mismatch between <see cref="Append"/> and <see cref="Clear"/> cannot arise.
/// </para>
/// </remarks>
internal static class RefreshTokenCookie
{
    /// <summary>
    /// The cookie name. The <c>__Secure-</c> prefix makes the browser reject the cookie unless it was
    /// set over HTTPS.
    /// </summary>
    public const string Name = "__Secure-cinedex_refresh_token";

    // The browser-visible path. Routes are registered without the base path because the pipeline
    // applies it via UsePathBase, so it has to be prepended here.
    private const string CookiePath = $"{ApiConstants.BasePath}/{ApiConstants.Auth.Route}";

    /// <summary>
    /// Writes the refresh token to the response as a hardened cookie.
    /// </summary>
    /// <param name="response">The response to append the cookie to.</param>
    /// <param name="refreshToken">The raw refresh token.</param>
    /// <param name="expiresAtUtc">The token's own expiry, so cookie and database agree on lifetime.</param>
    public static void Append(HttpResponse response, string refreshToken, DateTime expiresAtUtc)
    {
        var options = BuildOptions();
        options.Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc));

        response.Cookies.Append(Name, refreshToken, options);
    }

    /// <summary>
    /// Reads the refresh token from the request's cookies.
    /// </summary>
    /// <param name="request">The request to read the cookie from.</param>
    /// <returns>The raw refresh token, or <see langword="null"/> when the cookie is absent or blank.</returns>
    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken)
            ? refreshToken
            : null;

    /// <summary>
    /// Expires the refresh-token cookie so the browser stops sending it.
    /// </summary>
    /// <param name="response">The response to append the expiring cookie to.</param>
    public static void Clear(HttpResponse response) => response.Cookies.Delete(Name, BuildOptions());

    private static CookieOptions BuildOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,

        // Exempt from any future cookie-consent policy: authentication is not optional.
        IsEssential = true,
    };
}
