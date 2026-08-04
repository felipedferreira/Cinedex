namespace FoundryOceanus.Persistence.Abstractions.Exceptions;

/// <summary>
/// Base type for persistence failures that the application layer can meaningfully react to.
/// </summary>
/// <remarks>
/// <para>
/// Without this hierarchy, "the insert violated a unique index" reaches the application layer as a
/// <c>DbUpdateException</c> wrapping a <c>PostgresException</c> whose <c>SqlState</c> is the string
/// <c>"23505"</c>. Handling it means catching a provider type and comparing a magic number, in a
/// layer that is not supposed to know which database it is talking to — and every call site that
/// does it is a place that breaks when the provider changes its exception shape.
/// </para>
/// <para>
/// The provider-specific detail is not discarded. It is preserved on
/// <see cref="Exception.InnerException"/> for logs and diagnostics; it just stops being the thing
/// control flow depends on.
/// </para>
/// <para>
/// Only failures with a distinct <i>reaction</i> get a subclass. A unique-constraint violation
/// becomes 409 and a serialization failure gets retried, so both are worth naming; a syntax error in
/// generated SQL is a bug with no runtime response, so it stays an untranslated provider exception
/// rather than being dressed up as something the caller could handle.
/// </para>
/// </remarks>
public abstract class PersistenceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The provider exception this was translated from, if any.</param>
    protected PersistenceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets a value indicating whether retrying the operation unchanged could plausibly succeed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// True for failures caused by concurrent activity rather than by the request itself —
    /// serialization conflicts, deadlock victims, dropped connections. False for anything the same
    /// input would reproduce, such as a duplicate key or a missing foreign-key target.
    /// </para>
    /// <para>
    /// This is the signal <see cref="UnitOfWorkExtensions"/>.<c>ExecuteInTransactionAsync</c> uses to
    /// decide whether to retry, and it is the only thing a retry policy should be keying on. Deciding
    /// per call site whether a given SQLSTATE is worth retrying is how half the call sites end up
    /// deciding wrong.
    /// </para>
    /// </remarks>
    public virtual bool IsTransient => false;
}
