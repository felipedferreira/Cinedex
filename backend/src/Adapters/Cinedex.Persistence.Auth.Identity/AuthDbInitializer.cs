using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cinedex.Persistence.Auth.Identity;

/// <summary>
/// Applies pending migrations for the authentication database (the <c>auth</c> schema).
/// </summary>
public static class AuthDbInitializer
{
    /// <summary>
    /// Resolves the auth <see cref="Microsoft.EntityFrameworkCore.DbContext"/> from the supplied
    /// provider and applies any pending migrations.
    /// </summary>
    /// <param name="services">The application's root service provider.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when migrations have been applied.</returns>
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
