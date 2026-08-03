using Cinedex.Persistence.Abstractions.Exceptions;

namespace Cinedex.Persistence.EntityFrameworkCore;

/// <summary>
/// Turns a provider exception into one of the persistence exceptions the application layer knows how
/// to handle.
/// </summary>
/// <remarks>
/// <para>
/// Translators are tried in registration order and the first non-<see langword="null"/> result wins,
/// so provider-specific translators should be registered before more general ones. Registering
/// several is normal: the Entity Framework translator recognises failures EF itself raises, and a
/// provider translator recognises the driver's error codes underneath.
/// </para>
/// <para>
/// This is the one place that knows what SQLSTATE <c>23505</c> means. Everything downstream catches
/// <see cref="DuplicateKeyException"/>, which is the whole argument for having the abstraction: one
/// file changes when the provider changes its error reporting, not every call site that ever wrote a
/// row.
/// </para>
/// </remarks>
public interface IPersistenceExceptionTranslator
{
    /// <summary>
    /// Attempts to classify a provider exception.
    /// </summary>
    /// <remarks>
    /// Implementations must return <see langword="null"/> for anything they do not recognise, and in
    /// particular for <see cref="OperationCanceledException"/> — cancellation is the caller's own
    /// token doing its job, not a database failure, and translating it would convert a clean shutdown
    /// into an error.
    /// </remarks>
    /// <param name="exception">The exception thrown by Entity Framework or the underlying driver.</param>
    /// <returns>
    /// The translated exception, or <see langword="null"/> if this translator does not recognise
    /// <paramref name="exception"/> and the next translator should be tried.
    /// </returns>
    PersistenceException? Translate(Exception exception);
}
