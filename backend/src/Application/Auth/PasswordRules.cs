using FluentValidation;

namespace Cinedex.Application.Auth;

// Single home for password *input-shape* validation, shared by every validator that accepts a
// password. The rule shape (non-empty + bounded length) lives here so it is never spelled out twice;
// callers supply the per-field messages via ValidationMessages so the wording stays centralised too.
//
// Password *strength* (min length + complexity) is enforced by ASP.NET Core Identity and defined in
// PasswordPolicyConstants in the identity adapter. Do not add strength rules here — that would
// duplicate the authority and let the two drift.
internal static class PasswordRules
{
    // Maximum length accepted for a password input. Guards the hasher: an unbounded input could
    // trigger a very slow or memory-heavy hash. 256 comfortably exceeds every reasonable password.
    public const int MaxInputLength = 256;

    // Applies the shared password-input guard: non-empty and bounded. Both messages are required
    // so no rule ships with FluentValidation's generic default wording.
    public static IRuleBuilderOptions<T, string> PasswordInputGuard<T>(
        this IRuleBuilder<T, string> rule,
        string notEmptyMessage,
        string maxLengthMessage) =>
        rule
            .NotEmpty().WithMessage(notEmptyMessage)
            .MaximumLength(MaxInputLength).WithMessage(maxLengthMessage);
}
