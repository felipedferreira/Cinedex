namespace FoundryOceanus.Persistence.Abstractions.Exceptions;

/// <summary>
/// A write failed because of concurrent activity rather than because of anything wrong with the
/// request. Retrying the same operation can succeed.
/// </summary>
/// <remarks>
/// <para>
/// Covers serialization failures under
/// <see cref="Transactions.TransactionIsolationLevel.RepeatableRead"/> and
/// <see cref="Transactions.TransactionIsolationLevel.Serializable"/>, deadlock victims, and lost
/// connections. On PostgreSQL these are SQLSTATE 40001 and 40P01 in particular — the database's way
/// of saying "one of you has to go again", which is a normal part of running at those isolation
/// levels rather than an error condition.
/// </para>
/// <para>
/// The transaction is already dead when this is thrown. PostgreSQL aborts the whole transaction on a
/// serialization failure, so there is nothing to salvage — a retry must begin a new transaction and
/// redo the work, which is what
/// <see cref="UnitOfWorkExtensions"/>.<c>ExecuteInTransactionAsync</c> does when given a
/// <c>maxRetries</c> above zero.
/// </para>
/// </remarks>
public sealed class TransientPersistenceException : PersistenceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransientPersistenceException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The provider exception this was translated from, if any.</param>
    public TransientPersistenceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets a value indicating whether retrying could succeed. Always <see langword="true"/> for this
    /// type.
    /// </summary>
    public override bool IsTransient => true;
}
