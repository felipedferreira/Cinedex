using Cinedex.Persistence.Abstractions.Exceptions;
using Cinedex.Persistence.EntityFrameworkCore.Postgres;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cinedex.Persistence.Tests;

public sealed class NpgsqlExceptionTranslatorTests
{
    private readonly NpgsqlExceptionTranslator _translator = new();

    [Fact]
    public void Translate_WithUniqueViolation_ReturnsDuplicateKeyException()
    {
        PostgresException postgres = CreatePostgresException(
            PostgresErrorCodes.UniqueViolation,
            constraintName: "ix_widgets_name",
            tableName: "widgets");

        PersistenceException? translated = _translator.Translate(postgres);

        DuplicateKeyException duplicate = Assert.IsType<DuplicateKeyException>(translated);
        Assert.Equal("ix_widgets_name", duplicate.ConstraintName);
        Assert.Same(postgres, duplicate.InnerException);
        Assert.False(duplicate.IsTransient);
    }

    [Fact]
    public void Translate_WithForeignKeyViolation_ReturnsReferentialIntegrityException()
    {
        PostgresException postgres = CreatePostgresException(
            PostgresErrorCodes.ForeignKeyViolation,
            constraintName: "fk_widgets_owner");

        PersistenceException? translated = _translator.Translate(postgres);

        ReferentialIntegrityException referential = Assert.IsType<ReferentialIntegrityException>(translated);
        Assert.Equal("fk_widgets_owner", referential.ConstraintName);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.SerializationFailure)]
    [InlineData(PostgresErrorCodes.DeadlockDetected)]
    [InlineData(PostgresErrorCodes.StatementCompletionUnknown)]
    [InlineData(PostgresErrorCodes.LockNotAvailable)]
    public void Translate_WithConcurrencyCode_ReturnsTransientException(string sqlState)
    {
        PersistenceException? translated = _translator.Translate(CreatePostgresException(sqlState));

        TransientPersistenceException transient = Assert.IsType<TransientPersistenceException>(translated);
        Assert.True(transient.IsTransient);
    }

    // Retrying a statement the server timed out would repeat the timeout and add load. The
    // classification is what stops ExecuteInTransactionAsync from doing that on its own.
    [Fact]
    public void Translate_WithQueryCanceled_ReturnsNonTransientException()
    {
        PersistenceException? translated = _translator.Translate(
            CreatePostgresException(PostgresErrorCodes.QueryCanceled));

        UnclassifiedPersistenceException unclassified =
            Assert.IsType<UnclassifiedPersistenceException>(translated);
        Assert.False(unclassified.IsTransient);
    }

    [Fact]
    public void Translate_WithUnmappedSqlState_ReturnsUnclassifiedRatherThanNull()
    {
        // Claiming everything is deliberate: anything left untranslated would surface in application
        // code as an Npgsql type, which is the coupling the library exists to prevent.
        PersistenceException? translated = _translator.Translate(CreatePostgresException("42601"));

        Assert.IsType<UnclassifiedPersistenceException>(translated);
    }

    [Fact]
    public void Translate_WithViolationWrappedInDbUpdateException_UnwrapsToFindTheSqlState()
    {
        // The shape EF actually produces: SaveChanges wraps the provider failure, so a translator that
        // only inspected the outermost exception would classify nothing.
        PostgresException postgres = CreatePostgresException(
            PostgresErrorCodes.UniqueViolation,
            constraintName: "ix_widgets_name");
        var wrapped = new DbUpdateException("An error occurred while saving.", postgres);

        PersistenceException? translated = _translator.Translate(wrapped);

        DuplicateKeyException duplicate = Assert.IsType<DuplicateKeyException>(translated);
        Assert.Equal("ix_widgets_name", duplicate.ConstraintName);
    }

    [Fact]
    public void Translate_WithOperationCanceledException_ReturnsNull()
    {
        // Cancellation is the caller's token doing its job. Translating it would turn a clean shutdown
        // into a logged database failure, and — since 57014 can ride along inside it — a retryable one.
        PersistenceException? translated = _translator.Translate(
            new OperationCanceledException("Cancelled.", CreatePostgresException(PostgresErrorCodes.QueryCanceled)));

        Assert.Null(translated);
    }

    [Fact]
    public void Translate_WithUnrelatedException_ReturnsNullSoTheNextTranslatorRuns()
    {
        Assert.Null(_translator.Translate(new InvalidOperationException("Not a database failure.")));
    }

    private static PostgresException CreatePostgresException(
        string sqlState,
        string? constraintName = null,
        string? tableName = null) =>
        new(
            messageText: $"simulated {sqlState}",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: sqlState,
            tableName: tableName,
            constraintName: constraintName);
}
