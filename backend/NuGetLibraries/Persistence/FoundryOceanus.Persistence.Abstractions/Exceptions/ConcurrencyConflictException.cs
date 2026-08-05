namespace FoundryOceanus.Persistence.Abstractions.Exceptions;

/// <summary>
/// A row this unit of work expected to update or delete was changed or removed by someone else first,
/// and the optimistic concurrency check rejected the write.
/// </summary>
/// <remarks>
/// <para>
/// This is not transient in the retry-the-same-thing sense. The write was computed from a version of
/// the row that no longer exists, so replaying it would either fail identically or overwrite the
/// other party's change. Recovering means re-reading current state and deciding again — which is a
/// decision for the caller, not for a retry loop.
/// </para>
/// <para>
/// Usually surfaced to clients as HTTP 409.
/// </para>
/// </remarks>
public sealed class ConcurrencyConflictException : PersistenceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The provider exception this was translated from, if any.</param>
    public ConcurrencyConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
