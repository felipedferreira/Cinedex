using FoundryOceanus.Persistence.Abstractions.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FoundryOceanus.Persistence.EntityFrameworkCore;

/// <summary>
/// Translates the failures Entity Framework raises itself, independently of any provider.
/// </summary>
/// <remarks>
/// <para>
/// Registered last in the chain so that provider translators get first refusal — they can read
/// driver error codes and classify precisely, where this one can only see EF's own exception types.
/// </para>
/// <para>
/// Its final rule is the important one: any remaining <see cref="DbUpdateException"/> becomes an
/// <see cref="UnclassifiedPersistenceException"/> rather than being left alone. Letting it through
/// untranslated would mean an EF type escaping into the application layer on precisely the paths
/// nobody anticipated, which is where a leaky abstraction does its damage.
/// </para>
/// </remarks>
public sealed class EntityFrameworkExceptionTranslator : IPersistenceExceptionTranslator
{
    /// <inheritdoc />
    public PersistenceException? Translate(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            // Cancellation is the caller's token working as intended. It is not a database failure and
            // must never be dressed up as one, or a graceful shutdown starts looking like an outage.
            OperationCanceledException => null,

            // EF raises this when a conditional UPDATE or DELETE matched no rows — the row was changed
            // or deleted by someone else after we read it.
            DbUpdateConcurrencyException => new ConcurrencyConflictException(
                "A row this unit of work expected to modify was changed or deleted by another caller first. "
                + "Re-read the current state and decide again; replaying the same write would either fail identically "
                + "or overwrite the other change.",
                exception),

            DbUpdateException => new UnclassifiedPersistenceException(
                "The database rejected a write. No registered translator recognised the underlying error, so it "
                + "could not be classified further — see the inner exception for the provider's own report.",
                exception),

            _ => null,
        };
    }
}
