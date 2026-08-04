namespace FoundryOceanus.Persistence.Abstractions.Exceptions;

/// <summary>
/// A write violated a unique constraint or unique index.
/// </summary>
/// <remarks>
/// <para>
/// Worth catching rather than preventing. Checking "does this email already exist?" before inserting
/// is a race — another request can insert between the check and the write — so the constraint is the
/// only real guarantee, and this exception is how it reports back. Catching it is the correct
/// implementation of the rule, not a fallback for when the check was forgotten.
/// </para>
/// <para>
/// Usually surfaced to clients as HTTP 409.
/// </para>
/// </remarks>
public sealed class DuplicateKeyException : PersistenceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateKeyException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="constraintName">The violated constraint's name, if the provider reported one.</param>
    /// <param name="innerException">The provider exception this was translated from, if any.</param>
    public DuplicateKeyException(string message, string? constraintName = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ConstraintName = constraintName;
    }

    /// <summary>
    /// Gets the name of the violated constraint, or <see langword="null"/> if the provider did not
    /// report one.
    /// </summary>
    /// <remarks>
    /// Lets a handler distinguish two unique constraints on the same table — "this username is taken"
    /// against "this email is taken" — without parsing the message. Matching on it couples your code
    /// to a name defined in a migration, so treat that name as part of your schema's contract if you
    /// do.
    /// </remarks>
    public string? ConstraintName { get; }
}
