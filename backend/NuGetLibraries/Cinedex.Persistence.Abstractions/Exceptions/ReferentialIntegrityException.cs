namespace Cinedex.Persistence.Abstractions.Exceptions;

/// <summary>
/// A write violated a foreign-key constraint: it referenced a row that does not exist, or deleted a
/// row that others still reference.
/// </summary>
/// <remarks>
/// The direction matters when choosing a response. An insert naming a missing parent is usually a bad
/// request (422 or 404 on the referenced resource); a delete blocked by surviving children is usually
/// a conflict (409). The provider does not reliably distinguish the two, so the constraint name is
/// generally what you have to work with.
/// </remarks>
public sealed class ReferentialIntegrityException : PersistenceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferentialIntegrityException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="constraintName">The violated constraint's name, if the provider reported one.</param>
    /// <param name="innerException">The provider exception this was translated from, if any.</param>
    public ReferentialIntegrityException(string message, string? constraintName = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ConstraintName = constraintName;
    }

    /// <summary>
    /// Gets the name of the violated constraint, or <see langword="null"/> if the provider did not
    /// report one.
    /// </summary>
    public string? ConstraintName { get; }
}
