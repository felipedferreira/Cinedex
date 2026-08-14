using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Cinedex.WebService.Extensions;

/// <summary>
/// Extension methods for reading the authenticated caller's identity off a <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the id of the user the presented access token was issued for.
    /// </summary>
    /// <param name="principal">The authenticated caller.</param>
    /// <param name="userId">
    /// The caller's id, or <see cref="Guid.Empty"/> when there is no subject claim to read or it does
    /// not parse.
    /// </param>
    /// <returns><see langword="true"/> when a user id was found and parsed.</returns>
    /// <remarks>
    /// Both spellings of the subject are accepted deliberately. JWT bearer renames the token's RFC
    /// 7519 <c>sub</c> to <see cref="ClaimTypes.NameIdentifier"/> under its default inbound claim
    /// mapping and leaves it as <c>sub</c> with that mapping switched off, so reading only one
    /// spelling makes every caller's identity depend on a global authentication switch — and fails
    /// silently when it moves, because a claim that is not found looks exactly like a caller who has
    /// none. Accepting either costs one null-coalesce and removes the coupling.
    /// </remarks>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(subject, out userId);
    }
}
