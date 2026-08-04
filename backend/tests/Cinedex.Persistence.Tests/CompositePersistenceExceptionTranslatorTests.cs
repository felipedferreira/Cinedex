using FoundryOceanus.Persistence.Abstractions.Exceptions;
using FoundryOceanus.Persistence.EntityFrameworkCore;
using FoundryOceanus.Persistence.EntityFrameworkCore.Postgres;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cinedex.Persistence.Tests;

public sealed class CompositePersistenceExceptionTranslatorTests
{
    // The ordering guarantee the registration extensions rely on. Register the Entity Framework
    // translator first and its DbUpdateException catch-all would swallow every provider failure before
    // the PostgreSQL translator ever saw a SQLSTATE — every duplicate key would arrive unclassified,
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
    public void Translate_WithProviderTranslatorLast_FallsBackToTheCatchAll()
    {
        var composite = new CompositePersistenceExceptionTranslator(
            [new EntityFrameworkExceptionTranslator(), new NpgsqlExceptionTranslator()]);

        var wrapped = new DbUpdateException(
            "An error occurred while saving.",
            CreateUniqueViolation());

        Assert.IsType<UnclassifiedPersistenceException>(composite.Translate(wrapped));
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
}
