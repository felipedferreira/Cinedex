namespace Cinedex.Auth.Identity.Constants;

// The password policy enforced by ASP.NET Core Identity, defined in one place so the whole policy is
// obvious at a glance. Applied to PasswordOptions in DependencyInjection.AddAuthenticationAdapter.
internal static class PasswordPolicyConstants
{
    // Minimum number of characters.
    public const int MinimumLength = 8;

    // At least one digit (0-9).
    public const bool RequireDigit = true;

    // At least one lowercase letter (a-z).
    public const bool RequireLowercase = true;

    // At least one uppercase letter (A-Z).
    public const bool RequireUppercase = true;

    // At least one non-alphanumeric (special) character.
    public const bool RequireNonAlphanumeric = true;
}
