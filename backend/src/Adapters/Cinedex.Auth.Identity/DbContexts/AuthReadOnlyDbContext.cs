using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cinedex.Auth.Identity.DbContexts;

/// <summary>
/// The read-only half of the auth persistence split. Maps the same model as
/// <see cref="AuthDbContext"/>, but binds to <c>ConnectionStrings:ReadOnlyConnection</c> so it can be
/// pointed at a PostgreSQL role holding nothing but <c>SELECT</c>.
/// </summary>
/// <remarks>
/// <para>
/// Two independent guards, deliberately. The database role is the real one: it is the only thing an
/// exploited read path cannot argue its way past, and it keeps working no matter what future code
/// does with this context. The <c>SaveChanges</c> overrides below are the local one — they turn a
/// write attempted through here into a loud failure at the call site during development, rather than
/// a permission-denied error discovered in production.
/// </para>
/// <para>
/// Neither guard covers <c>ExecuteUpdate</c>/<c>ExecuteDelete</c>, which bypass the SaveChanges
/// pipeline entirely. That is why <see cref="Cinedex.Auth.Identity.Persistence.Query.IRefreshTokenQueries"/> returns materialised
/// snapshots and never hands an <see cref="IQueryable{T}"/> back to a caller: without a queryable
/// there is no surface to call them on.
/// </para>
/// <para>
/// This context has no migrations and never will — <see cref="AuthDbContext"/> owns the schema.
/// Never pass it to <c>dotnet ef migrations add</c>.
/// </para>
/// </remarks>
/// <param name="options">The options to configure the context with.</param>
internal sealed class AuthReadOnlyDbContext(DbContextOptions<AuthReadOnlyDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    private const string ReadOnlyMessage =
        "AuthReadOnlyDbContext is read-only. Route writes through IRefreshTokenRepository, which uses AuthDbContext.";

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // SaveChanges() and SaveChangesAsync(CancellationToken) both delegate to these two overloads, so
    // overriding the acceptAllChangesOnSuccess forms closes all four entry points at once.
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException(ReadOnlyMessage);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(ReadOnlyMessage);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        AuthModel.Configure(modelBuilder);
    }
}
