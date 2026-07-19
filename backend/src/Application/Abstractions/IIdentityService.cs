using Cinedex.Domain.UserAggregate;

namespace Cinedex.Application.Abstractions;

/// <summary>
/// Port for identity and account operations backed by an external identity provider (ASP.NET Core
/// Identity in the adapter). Speaks in domain <see cref="User"/> terms.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Registers a new user with the identity provider.
    /// </summary>
    /// <param name="email">The new user's email address. Must be unique.</param>
    /// <param name="userName">The new user's display name.</param>
    /// <param name="password">The plain-text password. Subject to the provider's password policy.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task producing the newly registered <see cref="User"/>.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// The identity provider rejected the request, for example a duplicate email or a
    /// password-policy failure.
    /// </exception>
    Task<User> RegisterAsync(string email, string userName, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the supplied credentials, recording the attempt against the provider's lockout policy.
    /// </summary>
    /// <param name="email">The email address identifying the account.</param>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task producing the authenticated <see cref="User"/>.</returns>
    /// <exception cref="Cinedex.Application.Exceptions.InvalidCredentialsException">
    /// The email is unknown, the password is wrong, or the account is locked out. The same exception
    /// is used for all three so callers cannot distinguish them and leak account existence.
    /// </exception>
    Task<User> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a password-reset token for the account with the given email.
    /// </summary>
    /// <param name="email">The email address identifying the account.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task producing the reset token, or <see langword="null"/> when no account exists for the
    /// email. Callers must respond identically in both cases to avoid account enumeration.
    /// </returns>
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Resets the account's password using a token previously issued by
    /// <see cref="GeneratePasswordResetTokenAsync"/>.
    /// </summary>
    /// <param name="email">The email address identifying the account.</param>
    /// <param name="resetToken">The reset token issued for this account.</param>
    /// <param name="newPassword">The new plain-text password. Subject to the provider's password policy.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the password has been reset.</returns>
    /// <exception cref="FluentValidation.ValidationException">
    /// The reset token or the new password was rejected. Also thrown when no account exists for the
    /// email, so the failure is indistinguishable from an invalid token.
    /// </exception>
    Task ResetPasswordAsync(string email, string resetToken, string newPassword, CancellationToken cancellationToken);
}