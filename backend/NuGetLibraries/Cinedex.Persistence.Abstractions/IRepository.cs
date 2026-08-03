namespace Cinedex.Persistence.Abstractions;

/// <summary>
/// Marks an interface as a repository that <see cref="IUnitOfWork.Repository{TRepository}"/> can resolve.
/// </summary>
/// <remarks>
/// <para>
/// This is a marker interface: it declares no members, and it deliberately never will. It exists so
/// that <see cref="IUnitOfWork.Repository{TRepository}"/> can constrain its type argument to things
/// the unit of work is actually meant to hand out, rather than accepting any registered service.
/// </para>
/// <para>
/// <b>There is deliberately no <c>IRepository&lt;TEntity&gt;</c> with <c>GetAll</c>/<c>Add</c>/
/// <c>Update</c>/<c>Delete</c> on it.</b> A generic CRUD repository is the ORM's vocabulary wearing a
/// different name — it re-exposes the same unconstrained surface you were trying to narrow, and every
/// caller ends up composing queries against it, which puts persistence knowledge back into the
/// application layer. Write one interface per aggregate, named for the domain, with methods named for
/// the operations the domain actually performs:
/// </para>
/// <code>
/// public interface IRefreshTokenRepository : IRepository
/// {
///     Task&lt;RefreshToken?&gt; FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
///
///     Task&lt;int&gt; RotateAsync(string tokenHash, DateTime rotatedAtUtc, string replacementHash, CancellationToken cancellationToken);
/// }
/// </code>
/// <para>
/// The test for whether an interface belongs here: read its method names out loud. If they sound like
/// things your product does, it is a repository. If they sound like things a database does, it is a
/// thin wrapper and it is costing you more than it returns.
/// </para>
/// </remarks>
public interface IRepository;
