using Cinedex.Domain.UserAggregate;

namespace Cinedex.Application.Abstractions;

// Port for identity/account operations backed by an external identity provider
// (ASP.NET Core Identity in the adapter). Speaks in domain User terms.
public interface IIdentityService
{
    // Registers a new user. Throws FluentValidation.ValidationException when the identity provider
    // rejects the request (e.g. duplicate email or password-policy failure).
    Task<User> RegisterAsync(string email, string userName, string password, CancellationToken cancellationToken);

    // Validates the supplied credentials. Throws InvalidCredentialsException when they are invalid.
    Task<User> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    // Generates a password-reset token for the user, or null when no user exists for the email
    // (callers should respond generically to avoid account enumeration).
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken);

    // Resets the user's password using a previously issued reset token. Throws
    // FluentValidation.ValidationException when the token or new password is rejected.
    Task ResetPasswordAsync(string email, string resetToken, string newPassword, CancellationToken cancellationToken);
}
