using FoundryOceanus.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoundryOceanus.Persistence.EntityFrameworkCore.DependencyInjection;

/// <summary>
/// Configures the unit of work for one persistence context. Handed to the callback on
/// <c>AddUnitOfWork</c>.
/// </summary>
/// <typeparam name="TContext">The persistence context being configured.</typeparam>
public sealed class UnitOfWorkBuilder<TContext>
    where TContext : DbContext
{
    private readonly UnitOfWorkRegistration<TContext> _registration;

    internal UnitOfWorkBuilder(IServiceCollection services, UnitOfWorkRegistration<TContext> registration)
    {
        Services = services;
        _registration = registration;
    }

    /// <summary>
    /// Gets the service collection being configured, for registrations this builder does not cover.
    /// </summary>
    public IServiceCollection Services { get; }

    internal object? Key { get; private set; }

    internal bool DefaultClaimRequested { get; private set; }

    /// <summary>
    /// Registers a repository that this unit of work can resolve.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers <typeparamref name="TImplementation"/> as <typeparamref name="TRepository"/> with a
    /// scoped lifetime, which is what puts it in the same scope as the unit of work and therefore on
    /// the same persistence context.
    /// </para>
    /// <para>
    /// The implementation should take <typeparamref name="TContext"/> as a constructor dependency, or
    /// derive from <see cref="EfRepository{TContext}"/>. What it must not do is obtain a context any
    /// other way — see <see cref="IDbContextBound"/>.
    /// </para>
    /// <para>
    /// Registered repositories stay injectable by constructor as normal. Registering here does not
    /// make <see cref="IUnitOfWork.Repository{TRepository}"/> the required way to reach them; it makes
    /// it a possible one.
    /// </para>
    /// </remarks>
    /// <typeparam name="TRepository">
    /// The repository interface — your own domain-named one, deriving from
    /// <see cref="IRepository"/>.
    /// </typeparam>
    /// <typeparam name="TImplementation">The implementing type.</typeparam>
    /// <returns>This builder, for chaining.</returns>
    public UnitOfWorkBuilder<TContext> AddRepository<TRepository, TImplementation>()
        where TRepository : class, IRepository
        where TImplementation : class, TRepository
    {
        Services.TryAddScoped<TRepository, TImplementation>();
        _registration.AddRepositoryType(typeof(TRepository));

        return this;
    }

    /// <summary>
    /// Sets the key this context's <see cref="IUnitOfWork"/> is registered under, for applications
    /// with more than one persistence context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <c>typeof(TContext)</c>. Supply your own key when the consuming code should not
    /// have to name an Entity Framework type to ask for its unit of work — an application-layer
    /// constant keeps that dependency pointing the right way:
    /// </para>
    /// <code>
    /// // Application layer, no Entity Framework anywhere in sight:
    /// public static class PersistenceKeys
    /// {
    ///     public const string Catalog = "catalog";
    ///     public const string Auth = "auth";
    /// }
    ///
    /// // Composition root:
    /// services.AddNpgsqlUnitOfWork&lt;AuthDbContext&gt;(uow =&gt; uow.WithKey(PersistenceKeys.Auth));
    ///
    /// // Consumer:
    /// public sealed class LogoutHandler([FromKeyedServices(PersistenceKeys.Auth)] IUnitOfWork unitOfWork);
    /// </code>
    /// </remarks>
    /// <param name="key">The service key.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public UnitOfWorkBuilder<TContext> WithKey(object key)
    {
        ArgumentNullException.ThrowIfNull(key);

        Key = key;

        return this;
    }

    /// <summary>
    /// Claims the unkeyed <see cref="IUnitOfWork"/> registration for this context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first context registered claims this automatically, so a single-context application never
    /// needs to call it — <see cref="IUnitOfWork"/> just resolves. Call it explicitly when you have
    /// several contexts and want a specific one to be the default rather than whichever happens to be
    /// registered first.
    /// </para>
    /// <para>
    /// Calling it from two contexts throws at startup instead of letting one silently win. A default
    /// unit of work that depends on registration order is the kind of thing that works in tests and
    /// writes to the wrong database in production.
    /// </para>
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public UnitOfWorkBuilder<TContext> AsDefault()
    {
        DefaultClaimRequested = true;

        return this;
    }

    /// <summary>
    /// Turns off the check that each resolved repository shares this unit of work's persistence
    /// context.
    /// </summary>
    /// <remarks>
    /// The check is one reference comparison per repository type per scope, so performance is not a
    /// reason to disable it. The real reason is a repository that legitimately holds a different
    /// context and manages its own transaction boundary — rare, and worth a comment where you call
    /// this. See <see cref="IDbContextBound"/> for what the check is protecting against.
    /// </remarks>
    /// <returns>This builder, for chaining.</returns>
    public UnitOfWorkBuilder<TContext> DisableRepositoryContextValidation()
    {
        _registration.ValidateRepositoryContext = false;

        return this;
    }
}
