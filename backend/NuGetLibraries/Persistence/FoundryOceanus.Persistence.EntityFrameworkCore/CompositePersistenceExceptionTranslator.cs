using FoundryOceanus.Persistence.Abstractions.Exceptions;

namespace FoundryOceanus.Persistence.EntityFrameworkCore;

/// <summary>
/// Runs every registered <see cref="IPersistenceExceptionTranslator"/> in order and returns the first
/// classification produced.
/// </summary>
/// <remarks>
/// <para>
/// This type is registered as itself rather than as an <see cref="IPersistenceExceptionTranslator"/>,
/// which is what stops it from resolving into its own list of translators.
/// </para>
/// <para>
/// <b>Order is the contract.</b> Translators run most specific first, so the PostgreSQL translator's
/// reading of SQLSTATE <c>23505</c> takes precedence over the Entity Framework translator's catch-all
/// for <c>DbUpdateException</c>. Provider and hand-written translators keep their registration order
/// relative to each other.
/// </para>
/// <para>
/// <b>The catch-all is sorted last here rather than registered last.</b> Leaving it to registration
/// order was a trap: <c>AddUnitOfWork</c> appends
/// <see cref="EntityFrameworkExceptionTranslator"/> on every call, so a single earlier
/// <c>AddUnitOfWork</c> — a second context, or the same context configured across two modules — put
/// the catch-all in front of <c>AddNpgsqlUnitOfWork</c>'s translator. It then claimed every
/// <c>DbUpdateException</c> before any SQLSTATE was read, and the whole classification layer degraded
/// to <see cref="UnclassifiedPersistenceException"/> without a word: no
/// <see cref="DuplicateKeyException"/> for a 409 to key on, and serialization failures no longer
/// transient, so retries stopped firing exactly where they were needed. Sorting here makes the
/// guarantee hold whatever order the composition root happens to use.
/// </para>
/// </remarks>
public sealed class CompositePersistenceExceptionTranslator : IPersistenceExceptionTranslator
{
    private readonly IPersistenceExceptionTranslator[] _translators;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositePersistenceExceptionTranslator"/> class.
    /// </summary>
    /// <param name="translators">
    /// The translators to consult. Their order is preserved except that
    /// <see cref="EntityFrameworkExceptionTranslator"/> is moved to the end — see the remarks on this
    /// class for why that is not left to the composition root.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="translators"/> is <see langword="null"/>.</exception>
    public CompositePersistenceExceptionTranslator(IEnumerable<IPersistenceExceptionTranslator> translators)
    {
        ArgumentNullException.ThrowIfNull(translators);

        // OrderBy is stable in LINQ to Objects, so everything other than the catch-all keeps the order
        // it was registered in and only the catch-all moves.
        _translators = [.. translators.OrderBy(translator => translator is EntityFrameworkExceptionTranslator)];
    }

    /// <inheritdoc />
    public PersistenceException? Translate(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        foreach (IPersistenceExceptionTranslator translator in _translators)
        {
            PersistenceException? translated = translator.Translate(exception);

            if (translated is not null)
            {
                return translated;
            }
        }

        return null;
    }
}
