namespace FoundryOceanus.Persistence.Abstractions.Exceptions;

/// <summary>
/// The database rejected a write for a reason no translator recognised.
/// </summary>
/// <remarks>
/// <para>
/// This type exists so that <c>catch (PersistenceException)</c> is a complete backstop. Without it,
/// an unrecognised failure would reach the application layer as whatever the ORM happened to throw —
/// which is the exact coupling the abstraction is there to prevent, showing up only on the paths
/// nobody tested.
/// </para>
/// <para>
/// Seeing one in your logs is a prompt, not a crisis: read <see cref="Exception.InnerException"/>,
/// and if the underlying condition is something callers should react to differently, add a case for
/// it to your translator. If it is a bug — a schema mismatch, a not-null violation on a column your
/// code should always populate — then a generic failure is the honest classification and there is
/// nothing to add.
/// </para>
/// </remarks>
public sealed class UnclassifiedPersistenceException : PersistenceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnclassifiedPersistenceException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The provider exception this was translated from, if any.</param>
    public UnclassifiedPersistenceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
