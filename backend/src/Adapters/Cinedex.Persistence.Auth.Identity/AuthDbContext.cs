using Cinedex.Persistence.Auth.Identity.Constants;
using Cinedex.Persistence.Auth.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.Auth.Identity;

// Identity user store (no role tables) plus refresh-token persistence, all under the "auth" schema.
internal sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(AuthDatabaseConstants.AuthSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
