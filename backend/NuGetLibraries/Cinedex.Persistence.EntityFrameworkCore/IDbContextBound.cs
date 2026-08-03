using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.EntityFrameworkCore;

/// <summary>
/// Exposes the persistence context a repository is bound to, so the unit of work can verify they are
/// the same one.
/// </summary>
/// <remarks>
/// <para>
/// Everything about this library rests on one invariant: <b>a unit of work and every repository it
/// hands out share a single <see cref="DbContext"/> instance</b>. That is what makes
/// <c>SaveChangesAsync</c> flush all of their writes together, and what makes their statements enlist
/// in a transaction opened on the unit of work.
/// </para>
/// <para>
/// The invariant holds automatically when a repository takes the context as a constructor dependency
/// and the container resolves both from the same scope. It breaks — silently, and only under load —
/// when a repository obtains a context some other way. Injecting an
/// <c>IDbContextFactory&lt;TContext&gt;</c> is the usual culprit: the factory hands out a fresh
/// context on a second connection, so the repository's writes commit on their own regardless of what
/// the surrounding transaction later decides. Nothing throws. The rollback simply does not cover
/// them.
/// </para>
/// <para>
/// Implementing this interface lets the unit of work check the invariant rather than trust it.
/// <see cref="EfRepository{TContext}"/> implements it for you, which is the main reason to derive
/// from that base class. Repositories that do not implement it are not rejected — they are just not
/// checked, and carry the invariant on their author's discretion.
/// </para>
/// </remarks>
public interface IDbContextBound
{
    /// <summary>
    /// Gets the persistence context this repository issues its statements against.
    /// </summary>
    DbContext DbContext { get; }
}
