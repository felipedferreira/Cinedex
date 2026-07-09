namespace Cinedex.Auth.Identity.Constants;

// The RBAC role names recognised by the auth adapter. Kept as string constants so both the seed
// data and any [Authorize(Roles = ...)] callers reference the same literal.
public static class RoleNames
{
    public const string User = "User";
    public const string Moderator = "Moderator";
    public const string Administrator = "Administrator";
}
