using FoundryOceanus.Persistence.EntityFrameworkCore.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoundryOceanus.Persistence.EntityFrameworkCore.Postgres.DependencyInjection;

/// <summary>
/// Registers the unit of work with PostgreSQL-aware exception translation.
/// </summary>
public static class NpgsqlUnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// Registers a unit of work over <typeparamref name="TContext"/> with PostgreSQL error
    /// classification enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything <c>AddUnitOfWork</c> does, plus <see cref="NpgsqlExceptionTranslator"/> at the front
    /// of the translator chain. Use this rather than <c>AddUnitOfWork</c> on PostgreSQL: without it,
    /// a unique-constraint violation reaches callers as a generic
    /// <c>UnclassifiedPersistenceException</c> instead of a <c>DuplicateKeyException</c> they can
    /// turn into a 409, and a serialization failure is not recognised as transient — so
    /// <c>ExecuteInTransactionAsync</c>'s retries never fire, silently, exactly when they matter.
    /// </para>
    /// <para>
    /// Call it after <c>AddDbContext&lt;TContext&gt;</c>; see <c>AddUnitOfWork</c> for why the
    /// ordering matters and what the scoped-lifetime requirement buys.
    /// </para>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;AuthDbContext&gt;(options =&gt; options
    ///     .UseNpgsql(connectionString)
    ///     .UseCamelCaseNamingConvention());
    ///
    /// services.AddNpgsqlUnitOfWork&lt;AuthDbContext&gt;(uow =&gt; uow
    ///     .AddRepository&lt;IRefreshTokenRepository, RefreshTokenRepository&gt;());
    /// </code>
    /// </example>
    /// </remarks>
    /// <typeparam name="TContext">The Entity Framework context to build a unit of work over.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Registers repositories and adjusts behaviour.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddNpgsqlUnitOfWork<TContext>(
        this IServiceCollection services,
        Action<UnitOfWorkBuilder<TContext>>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registered before AddUnitOfWork so the natural order is already right. It is no longer the
        // only thing keeping it right: CompositePersistenceExceptionTranslator sorts the Entity
        // Framework catch-all last regardless, because a composition root that called AddUnitOfWork
        // for any context first used to silently invert this and disable SQLSTATE classification.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersistenceExceptionTranslator, NpgsqlExceptionTranslator>());

        return services.AddUnitOfWork(configure);
    }
}
