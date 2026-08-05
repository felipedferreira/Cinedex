using FoundryOceanus.Persistence.Abstractions.Exceptions;
using FoundryOceanus.Persistence.EntityFrameworkCore;
using FoundryOceanus.Persistence.EntityFrameworkCore.Postgres;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoundryOceanus.Persistence.Tests;

public sealed class CompositePersistenceExceptionTranslatorTests
{
    // The ordering guarantee the registration extensions rely on. Register the Entity Framework
    // translator first and its DbUpdateException catch-all would swallow every provider failure before
    // the PostgreSQL translator ever saw a SQLSTATE â€” every duplicate key would arrive unclassified,
    // and no test of the PostgreSQL translator in isolation would notice.
    [Fact]
    public void Translate_WithProviderTranslatorFirst_PrefersTheSpecificClassification()
    {
        var composite = new CompositePersistenceExceptionTranslator(
            [new NpgsqlExceptionTranslator(), new EntityFrameworkExceptionTranslator()]);

        var wrapped = new DbUpdateException(
            "An error occurred while saving.",
            CreateUniqueViolation());

        Assert.IsType<DuplicateKeyException>(composite.Translate(wrapped));
    }

    [Fact]
    public void Translate_WithTheCatchAllHandedInFirst_StillPrefersTheSpecificClassification()
    {
        // The composite sorts the catch-all last rather than trusting the order it was handed. This
        // used to return UnclassifiedPersistenceException, and a composition root reached that state
        // just by calling AddUnitOfWork for any context before AddNpgsqlUnitOfWork — at which point
        // duplicate keys stopped being 409s and serialization failures stopped being retried, silently.
        var composite = new CompositePersistenceExceptionTranslator(
            [new EntityFrameworkExceptionTranslator(), new NpgsqlExceptionTranslator()]);

        var wrapped = new DbUpdateException(
            "An error occurred while saving.",
            CreateUniqueViolation());

        Assert.IsType<DuplicateKeyException>(composite.Translate(wrapped));
    }

    [Fact]
    public void Translate_PreservesTheOrderOfEverythingExceptTheCatchAll()
    {
        // Sinking the catch-all must not reshuffle the rest: a hand-registered translator that was
        // meant to run before Npgsql's still does.
        var first = new StubTranslator();
        var second = new StubTranslator();

        var composite = new CompositePersistenceExceptionTranslator(
            [new EntityFrameworkExceptionTranslator(), first, second]);

        composite.Translate(new DbUpdateException("An error occurred while saving.", CreateUniqueViolation()));

        Assert.True(first.WasConsulted);
        Assert.True(second.WasConsulted);
        Assert.True(
            first.ConsultedAt < second.ConsultedAt,
            "Translators other than the catch-all must keep their relative order.");
    }

    [Fact]
    public void Translate_WithNoTranslatorRecognisingTheException_ReturnsNull()
    {
        var composite = new CompositePersistenceExceptionTranslator(
            [new NpgsqlExceptionTranslator(), new EntityFrameworkExceptionTranslator()]);

        Assert.Null(composite.Translate(new InvalidOperationException("Not a database failure.")));
    }

    [Fact]
    public void Translate_WithNoTranslators_ReturnsNull()
    {
        var composite = new CompositePersistenceExceptionTranslator([]);

        Assert.Null(composite.Translate(CreateUniqueViolation()));
    }

    private static PostgresException CreateUniqueViolation() =>
        new(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            constraintName: "ix_widgets_name");

    /// <summary>
    /// Classifies nothing, and records when it was asked. Enough to observe consultation order.
    /// </summary>
    private sealed class StubTranslator : IPersistenceExceptionTranslator
    {
        private static int _sequence;

        public bool WasConsulted { get; private set; }

        public int ConsultedAt { get; private set; }

        public PersistenceException? Translate(Exception exception)
        {
            WasConsulted = true;
            ConsultedAt = Interlocked.Increment(ref _sequence);

            return null;
        }
    }
}
