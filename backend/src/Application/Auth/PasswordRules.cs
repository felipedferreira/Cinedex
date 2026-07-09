using FluentValidation;

namespace Cinedex.Application.Auth;

// Single home for password *input-shape* validation, shared by every validator that accepts a
// password. The rules live here and here only; validators call PasswordInputGuard() so no rule is
// spelled out twice.
//
// Password *strength* (min length + complexity) is enforced by ASP.NET Core Identity and defined in
// PasswordPolicyConstants in the identity adapter. Do not add strength rules here — that would
// duplicate the authority and let the two drift.
internal static class PasswordRules
{
    // Maximum length accepted for a password input. Guards the hasher: an unbounded input could
    // trigger a very slow or memory-heavy hash. 256 comfortably exceeds every reasonable password.
    public const int MaxInputLength = 256;

    // Applies the shared password-input guard: non-empty and bounded.
    public static IRuleBuilderOptions<T, string> PasswordInputGuard<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(MaxInputLength);
}
