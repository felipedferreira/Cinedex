using Cinedex.Application.Abstractions;
using Cinedex.Application.Exceptions;
using Cinedex.Domain.UserAggregate;
using Cinedex.Persistence.Auth.Identity.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;

namespace Cinedex.Persistence.Auth.Identity.Services;

internal sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<User> RegisterAsync(string email, string userName, string password, CancellationToken cancellationToken)
    {
        var applicationUser = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            UserName = userName,
        };

        var result = await userManager.CreateAsync(applicationUser, password);
        if (!result.Succeeded)
        {
            throw ToValidationException(result);
        }

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

        var result = await userManager.ResetPasswordAsync(applicationUser, resetToken, newPassword);
        if (!result.Succeeded)
        {
            throw ToValidationException(result);
        }
    }

    private static ValidationException ToValidationException(IdentityResult result)
    {
        var failures = result.Errors
            .Select(error => new ValidationFailure(error.Code, error.Description))
            .ToList();

        return new ValidationException(failures);
    }
}
