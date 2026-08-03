using Cinedex.Persistence.Abstractions.Exceptions;

namespace Cinedex.Persistence.EntityFrameworkCore;

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
/// <b>Order is the contract.</b> Translators run in registration order, most specific first, so the
/// PostgreSQL translator's reading of SQLSTATE <c>23505</c> takes precedence over the Entity Framework
/// translator's catch-all for <c>DbUpdateException</c>. The registration extensions arrange this;
/// anything you register by hand is appended to the end.
/// </para>
/// </remarks>
public sealed class CompositePersistenceExceptionTranslator : IPersistenceExceptionTranslator
{
    private readonly IPersistenceExceptionTranslator[] _translators;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositePersistenceExceptionTranslator"/> class.
    /// </summary>
    /// <param name="translators">The translators to consult, in priority order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="translators"/> is <see langword="null"/>.</exception>
    public CompositePersistenceExceptionTranslator(IEnumerable<IPersistenceExceptionTranslator> translators)
    {
        ArgumentNullException.ThrowIfNull(translators);

        _translators = [.. translators];
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
