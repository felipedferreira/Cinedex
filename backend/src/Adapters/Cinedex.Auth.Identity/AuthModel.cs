using Cinedex.Auth.Identity.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinedex.Auth.Identity;

// The auth model, described in exactly one place. Two contexts map it — AuthDbContext for writes and
// AuthReadOnlyDbContext for reads — and they must agree on every table, column and index, because they
// address the same rows over two different connections. Sharing this method is what makes that
// agreement structural rather than a convention someone has to remember.
internal static class AuthModel
{
    // Call after the IdentityDbContext base has run, so the schema default and the assembly's
    // IEntityTypeConfiguration types apply on top of Identity's own mappings.
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(AuthDatabaseConstants.AuthSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
