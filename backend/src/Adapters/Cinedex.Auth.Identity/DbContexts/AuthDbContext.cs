using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cinedex.Auth.Identity.DbContexts;

// Identity user + role store plus refresh-token persistence, all under the "auth" schema. This is
// the read-write half of the split: it owns migrations, and every write in the system goes through
// it. Reads that do not need to see uncommitted work in the current transaction belong on
// AuthReadOnlyDbContext instead.
internal sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        AuthModel.Configure(modelBuilder);
    }
}
