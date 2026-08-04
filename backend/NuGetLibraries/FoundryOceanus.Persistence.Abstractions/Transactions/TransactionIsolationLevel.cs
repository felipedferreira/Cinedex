namespace FoundryOceanus.Persistence.Abstractions.Transactions;

/// <summary>
/// The isolation levels a <see cref="ITransaction"/> can be opened at.
/// </summary>
/// <remarks>
/// <para>
/// This is intentionally not <see cref="System.Data.IsolationLevel"/>. That enum carries seven values
/// drawn from every database ADO.NET has ever targeted, and on PostgreSQL three of them are fictions:
/// <c>ReadUncommitted</c> is silently treated as <c>ReadCommitted</c> (PostgreSQL has no dirty reads
/// to permit), <c>Snapshot</c> does not exist under that name, and <c>Chaos</c> means nothing at all.
/// Offering values the database will quietly reinterpret is how an abstraction starts lying to the
/// people who read it.
/// </para>
/// <para>
/// The four values here are the four that PostgreSQL actually implements, so a level you select is a
/// level you get.
/// </para>
/// </remarks>
public enum TransactionIsolationLevel
{
    /// <summary>
    /// Use the provider's configured default. On PostgreSQL this is <see cref="ReadCommitted"/> unless
    /// the server's <c>default_transaction_isolation</c> has been changed.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Each statement sees rows committed before that statement began. The PostgreSQL default, and the
    /// right choice for ordinary request-scoped writes.
    /// </summary>
    ReadCommitted = 1,

    /// <summary>
    /// Every statement in the transaction sees the same snapshot, taken at the first statement.
    /// Protects against non-repeatable and phantom reads.
    /// </summary>
    /// <remarks>
    /// On PostgreSQL a write conflict at this level aborts the transaction with SQLSTATE 40001 rather
    /// than blocking. Callers must be prepared to retry — the <c>maxRetries</c> argument on
    /// <see cref="UnitOfWorkExtensions"/>.<c>ExecuteInTransactionAsync</c> exists for exactly this.
    /// </remarks>
    RepeatableRead = 2,

    /// <summary>
    /// Transactions behave as though they ran one after another. The strongest level, and the most
    /// likely to abort under contention.
    /// </summary>
    /// <remarks>
    /// Same caveat as <see cref="RepeatableRead"/>, more often: PostgreSQL detects serialization
    /// anomalies and aborts one participant with SQLSTATE 40001. Choosing this level without a retry
    /// policy converts a concurrency control mechanism into an intermittent 500.
    /// </remarks>
    Serializable = 3,
}
