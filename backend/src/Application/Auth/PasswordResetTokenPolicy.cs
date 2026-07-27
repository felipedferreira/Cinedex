namespace Cinedex.Application.Auth;

/// <summary>
/// How long a password-reset token stays valid, and the wording that states it to the recipient.
/// Single source of truth for the identity adapter that configures Identity's token expiry and for
/// the reset email that promises it, so the configured value and the copy cannot drift.
/// </summary>
public static class PasswordResetTokenPolicy
{
    /// <summary>How long a password-reset token stays valid, in whole hours.</summary>
    public const int LifespanHours = 1;

    /// <summary>Gets how long a password-reset token stays valid after it is issued.</summary>
    public static TimeSpan Lifespan => TimeSpan.FromHours(LifespanHours);

    /// <summary>
    /// Gets <see cref="Lifespan"/> as it reads in user-facing copy, for example <c>1 hour</c>. The
    /// lifespan is whole hours, so singular-or-plural is all the wording ever needs.
    /// </summary>
    public static string LifespanDescription =>
        LifespanHours == 1 ? "1 hour" : FormattableString.Invariant($"{LifespanHours} hours");
}
