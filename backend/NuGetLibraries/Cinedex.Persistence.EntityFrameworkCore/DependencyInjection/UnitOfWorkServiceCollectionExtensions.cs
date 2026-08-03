using Cinedex.Persistence.Abstractions;
using Cinedex.Persistence.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cinedex.Persistence.EntityFrameworkCore.DependencyInjection;

/// <summary>
/// Registers the unit of work and its repositories for an Entity Framework context.
/// </summary>
public static class UnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// Registers a unit of work over <typeparamref name="TContext"/>, along with the repositories it
    /// can resolve.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Call this after <c>AddDbContext&lt;TContext&gt;</c>.</b> The context must be registered with
    /// a scoped lifetime — a singleton or transient context breaks the one guarantee this library
    /// makes, that the unit of work and its repositories share an instance. If the context is already
    /// registered when this runs, its lifetime is checked and a wrong one throws immediately. If it is
    /// registered afterwards, that check cannot happen and the misconfiguration surfaces later.
    /// </para>
    /// <para>
    /// Registering PostgreSQL? Call <c>AddNpgsqlUnitOfWork</c> from
    /// <c>Cinedex.Persistence.EntityFrameworkCore.Postgres</c> instead. It does everything this does
    /// and adds SQLSTATE-level exception translation and advisory locks — without it, a unique-constraint
    /// violation arrives as a generic failure rather than as <c>DuplicateKeyException</c>.
    /// </para>
    /// <para>
    /// Calling this more than once for the same context is additive rather than an error, so
    /// composition can be split across modules. Each call contributes its repositories to the same
    /// registration.
    /// </para>
    /// <para>
    /// <b>What gets registered.</b> <see cref="EfUnitOfWork{TContext}"/> and
    /// <see cref="IUnitOfWorkScopeFactory"/> as scoped, both under a service key (see
    /// <see cref="UnitOfWorkBuilder{TContext}.WithKey"/>) and, for the first context registered, under
    /// the plain unkeyed interfaces as well. The exception translators are singletons.
    /// </para>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;AuthDbContext&gt;(options =&gt; options.UseNpgsql(connectionString));
    ///
    /// services.AddUnitOfWork&lt;AuthDbContext&gt;(uow =&gt; uow
    ///     .AddRepository&lt;IRefreshTokenRepository, RefreshTokenRepository&gt;()
    ///     .AddRepository&lt;IUserRepository, UserRepository&gt;());
    /// </code>
    /// </example>
    /// </remarks>
    /// <typeparam name="TContext">The Entity Framework context to build a unit of work over.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Registers repositories and adjusts behaviour.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TContext"/> is registered with a lifetime other than scoped, or a second
    /// context has called <see cref="UnitOfWorkBuilder{TContext}.AsDefault"/> when the unkeyed
    /// registration is already taken.
    /// </exception>
    public static IServiceCollection AddUnitOfWork<TContext>(
        this IServiceCollection services,
        Action<UnitOfWorkBuilder<TContext>>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        EnsureContextLifetimeIsScoped<TContext>(services);

        // Reuse the existing registration when this context has been configured already, so that two
        // modules each contributing their own repositories both end up in the same one. Creating a
        // second would mean the later TryAddSingleton silently discarded it, and the repositories it
        // recorded would be missing from the diagnostics without anything looking wrong.
        UnitOfWorkRegistration<TContext> registration = FindExistingRegistration<TContext>(services)
            ?? new UnitOfWorkRegistration<TContext>();

        var builder = new UnitOfWorkBuilder<TContext>(services, registration);
        configure?.Invoke(builder);

        services.TryAddSingleton(registration);
        services.TryAddSingleton<CompositePersistenceExceptionTranslator>();

        // Appended last so that provider translators registered earlier — by AddNpgsqlUnitOfWork, for
        // instance — get first refusal on an exception they can classify more precisely.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersistenceExceptionTranslator, EntityFrameworkExceptionTranslator>());

        services.TryAddScoped<EfUnitOfWork<TContext>>();
        services.TryAddScoped<EfUnitOfWorkScopeFactory<TContext>>();

        object serviceKey = builder.Key ?? typeof(TContext);

        services.TryAddKeyedScoped<IUnitOfWork>(
            serviceKey,
            (provider, _) => provider.GetRequiredService<EfUnitOfWork<TContext>>());
        services.TryAddKeyedScoped<IUnitOfWorkScopeFactory>(
            serviceKey,
            (provider, _) => provider.GetRequiredService<EfUnitOfWorkScopeFactory<TContext>>());

        RegisterDefaultIfAvailable<TContext>(services, builder.DefaultClaimRequested);

        return services;
    }

    private static UnitOfWorkRegistration<TContext>? FindExistingRegistration<TContext>(IServiceCollection services)
        where TContext : DbContext =>
        services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(UnitOfWorkRegistration<TContext>))
            ?.ImplementationInstance as UnitOfWorkRegistration<TContext>;

    private static void EnsureContextLifetimeIsScoped<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            candidate => candidate.ServiceType == typeof(TContext) && !candidate.IsKeyedService);

        // Not registered yet is not an error: AddUnitOfWork before AddDbContext is a reasonable order
        // to write things in. The container reports the missing registration clearly enough on its
        // own when the unit of work is first resolved. A wrong lifetime is the case worth catching,
        // because the container is perfectly happy with it and the damage is silent.
        if (descriptor is null || descriptor.Lifetime == ServiceLifetime.Scoped)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{typeof(TContext).Name} is registered as {descriptor.Lifetime}, but the unit of work requires a scoped "
            + "context. Everything here depends on the unit of work and its repositories resolving the same instance "
            + $"within one scope: a singleton {typeof(TContext).Name} would be shared across concurrent requests — Entity "
            + "Framework contexts are not thread-safe — and a transient one would give every repository a separate "
            + "context on a separate connection, so their writes would fall outside each other's transactions. Register "
            + "it with AddDbContext, which is scoped by default.");
    }

    private static void RegisterDefaultIfAvailable<TContext>(IServiceCollection services, bool claimRequested)
        where TContext : DbContext
    {
        bool alreadyClaimed = services.Any(
            descriptor => descriptor.ServiceType == typeof(IUnitOfWork) && !descriptor.IsKeyedService);

        if (alreadyClaimed)
        {
            if (claimRequested)
            {
                throw new InvalidOperationException(
                    $"AsDefault() was called for {typeof(TContext).Name}, but another persistence context has already "
                    + "claimed the unkeyed IUnitOfWork registration. Only one context can be the default. Give this one a "
                    + "key with WithKey(...) and resolve it with [FromKeyedServices(...)], or move AsDefault() to "
                    + "whichever context should own the plain IUnitOfWork.");
            }

            // Silently leaving the default with the first context is the right call when nobody asked
            // for it: the second context is still reachable by key, so nothing is lost, and throwing
            // would make adding a second context a breaking change for code that never mentioned it.
            return;
        }

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<EfUnitOfWork<TContext>>());
        services.AddScoped<IUnitOfWorkScopeFactory>(
            provider => provider.GetRequiredService<EfUnitOfWorkScopeFactory<TContext>>());
    }
}
