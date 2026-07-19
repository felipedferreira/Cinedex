using Cinedex.Application.Abstractions;
using Cinedex.Application.Exceptions;
using Cinedex.Auth.Identity.Constants;
using Cinedex.Auth.Identity.Entities;
using Cinedex.Domain.UserAggregate;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cinedex.Auth.Identity.Services;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    AuthDbContext dbContext) : IIdentityService
{
    public async Task<User> RegisterAsync(string email, string userName, string password, CancellationToken cancellationToken)
    {
        var applicationUser = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            UserName = userName,
        };

        IdentityResult result;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            result = await userManager.CreateAsync(applicationUser, password);
        }
        catch (DbUpdateException exception) when (IsUniqueEmailViolation(exception))
        {
            throw ToDuplicateEmailValidationException(email);
        }

        if (!result.Succeeded)
        {
            throw ToValidationException(result);
        }

        // Every new account is placed in the baseline User role. A failure here means the user was
        // created but is roleless — a server-side inconsistency, not a validation problem.
        var roleResult = await userManager.AddToRoleAsync(applicationUser, RoleNames.User);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to assign default role to new user: {errors}");
        }

        await transaction.CommitAsync(cancellationToken);

        return applicationUser.ToDomainUser();
    }

    public async Task<User> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var applicationUser = await userManager.FindByEmailAsync(email);
        if (applicationUser is null)
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        if (await userManager.IsLockedOutAsync(applicationUser))
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        if (!await userManager.CheckPasswordAsync(applicationUser, password))
        {
            await userManager.AccessFailedAsync(applicationUser);
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(applicationUser);

        return applicationUser.ToDomainUser();
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken)
    {
        var applicationUser = await userManager.FindByEmailAsync(email);
        if (applicationUser is null)
        {
            return null;
        }

        return await userManager.GeneratePasswordResetTokenAsync(applicationUser);
    }

    public async Task ResetPasswordAsync(string email, string resetToken, string newPassword, CancellationToken cancellationToken)
    {
        var applicationUser = await userManager.FindByEmailAsync(email);
        if (applicationUser is null)
        {
            // Do not reveal whether the account exists; surface a generic validation error.
            throw new ValidationException([new ValidationFailure("ResetToken", "The password reset request is invalid.")]);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await userManager.ResetPasswordAsync(applicationUser, resetToken, newPassword);
        if (!result.Succeeded)
        {
            throw ToValidationException(result);
        }

        var revokedAtUtc = DateTime.UtcNow;
        await dbContext.RefreshTokens
            .Where(token => token.UserId == applicationUser.Id && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAtUtc, revokedAtUtc),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static bool IsUniqueEmailViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException &&
        postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
        string.Equals(postgresException.ConstraintName, "EmailIndex", StringComparison.Ordinal);

    private static ValidationException ToDuplicateEmailValidationException(string email) =>
        new([new ValidationFailure("DuplicateEmail", $"Email '{email}' is already taken.")]);

    private static ValidationException ToValidationException(IdentityResult result)
    {
        var failures = result.Errors
            .Select(error => new ValidationFailure(error.Code, error.Description))
            .ToList();

        return new ValidationException(failures);
    }
}