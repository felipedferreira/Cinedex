using Cinedex.Persistence.Abstractions.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.EntityFrameworkCore;

/// <summary>
/// Optional base class for repositories that persist through Entity Framework. Supplies the shared
/// context and exception translation, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// Deriving from this is not required. A repository that takes <typeparamref name="TContext"/> as a
/// constructor dependency already participates correctly in the unit of work, and if that is all you
/// need then a plain class is one fewer thing between you and the code. What the base class adds is
/// two things you would otherwise repeat:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// It implements <see cref="IDbContextBound"/>, so the unit of work can verify at runtime that this
/// repository really does share its context — see that interface for why silence on this point is
/// expensive.
/// </description>
/// </item>
/// <item>
/// <description>
/// It offers <see cref="ExecuteAsync{TResult}"/>, which applies the same exception translation to
/// direct statements that the unit of work applies to saves. Set-based operations such as
/// <c>ExecuteUpdateAsync</c> and <c>ExecuteDeleteAsync</c> never pass through <c>SaveChanges</c>, so
/// without this their failures would reach callers as raw provider exceptions.
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>Note what is not here.</b> There is no <c>GetAll</c>, no <c>Add</c>, no <c>Query</c>, no
/// <c>DbSet</c> exposed to derived types beyond what <see cref="DbContext"/> already gives them. This
/// is a base class for sharing plumbing, not a generic repository — see <see cref="Abstractions.IRepository"/>
/// for why that distinction is worth keeping.
/// </para>
/// <example>
/// <code>
/// internal sealed class RefreshTokenRepository(
///     AuthDbContext dbContext,
///     CompositePersistenceExceptionTranslator translator)
///     : EfRepository&lt;AuthDbContext&gt;(dbContext, translator), IRefreshTokenRepository
/// {
///     public Task&lt;RefreshToken?&gt; FindByTokenHashAsync(string tokenHash, CancellationToken ct) =&gt;
///         ExecuteAsync(
///             token =&gt; DbContext.RefreshTokens
///                 .AsNoTracking()
///                 .FirstOrDefaultAsync(entity =&gt; entity.TokenHash == tokenHash, token),
///             ct);
/// }
/// </code>
/// </example>
/// </remarks>
/// <typeparam name="TContext">The Entity Framework context this repository reads and writes through.</typeparam>
public abstract class EfRepository<TContext> : IDbContextBound
    where TContext : DbContext
{
    private readonly CompositePersistenceExceptionTranslator _exceptionTranslator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfRepository{TContext}"/> class.
    /// </summary>
    /// <param name="dbContext">
    /// The scoped persistence context. Take this as a constructor dependency and let the container
    /// supply it — resolving one from an <c>IDbContextFactory</c> instead is what
    /// <see cref="IDbContextBound"/> exists to detect.
    /// </param>
    /// <param name="exceptionTranslator">Turns provider failures into persistence exceptions.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    protected EfRepository(TContext dbContext, CompositePersistenceExceptionTranslator exceptionTranslator)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(exceptionTranslator);

        DbContext = dbContext;
        _exceptionTranslator = exceptionTranslator;
    }

    /// <inheritdoc />
    DbContext IDbContextBound.DbContext => this.DbContext;

    /// <summary>
    /// Gets the persistence context, shared with the unit of work for this scope.
    /// </summary>
    protected TContext DbContext { get; }

    /// <summary>
    /// Runs a database operation, translating any provider failure into a
    /// <see cref="Abstractions.Exceptions.PersistenceException"/>.
    /// </summary>
    /// <remarks>
    /// Worth wrapping anything that reaches the database without going through
    /// <see cref="Abstractions.IUnitOfWork.SaveChangesAsync"/> — queries, and the set-based
    /// <c>ExecuteUpdateAsync</c> and <c>ExecuteDeleteAsync</c> statements. Writes made by tracking an
    /// entity and saving later are already covered by the unit of work and need nothing here.
    /// </remarks>
    /// <typeparam name="TResult">The operation's result type.</typeparam>
    /// <param name="operation">The operation to run.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The operation's result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    protected async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            return await operation(cancellationToken);
        }
        catch (Exception exception)
        {
            // Not an exception filter, for the reason spelled out on EfUnitOfWork.SaveChangesAsync:
            // .NET swallows exceptions thrown inside filters, so a buggy translator would silently
            // classify nothing.
            PersistenceException? translated = _exceptionTranslator.Translate(exception);

            if (translated is null)
            {
                throw;
            }

            throw translated;
        }
    }

    /// <summary>
    /// Runs a database operation, translating any provider failure into a
    /// <see cref="Abstractions.Exceptions.PersistenceException"/>.
    /// </summary>
    /// <param name="operation">The operation to run.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that completes when the operation has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    protected async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            await operation(cancellationToken);
        }
        catch (Exception exception)
        {
            PersistenceException? translated = _exceptionTranslator.Translate(exception);

            if (translated is null)
            {
                throw;
            }

            throw translated;
        }
    }
}
