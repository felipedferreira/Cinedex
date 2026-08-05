using FoundryOceanus.Persistence.Abstractions;
using FoundryOceanus.Persistence.Abstractions.Exceptions;
using FoundryOceanus.Persistence.EntityFrameworkCore;
using FoundryOceanus.Persistence.EntityFrameworkCore.DependencyInjection;
using FoundryOceanus.Persistence.EntityFrameworkCore.Postgres.DependencyInjection;
using FoundryOceanus.Persistence.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoundryOceanus.Persistence.Tests;

/// <summary>
/// Registration-time behaviour. None of these touch a database â€” they assert on the shape of the
/// service collection and on the failures it refuses to accept.
/// </summary>
public sealed class UnitOfWorkRegistrationTests
{
    private const string ConnectionString = "Host=localhost;Database=unused;Username=unused;Password=unused";

    [Fact]
    public void AddUnitOfWork_WithScopedContext_RegistersTheDefaultUnitOfWork()
    {
        ServiceCollection services = CreateServicesWithContext();

        services.AddUnitOfWork<WidgetDbContext>(uow => uow.AddRepository<IWidgetRepository, WidgetRepository>());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IUnitOfWork>());
        Assert.NotNull(scope.ServiceProvider.GetService<IUnitOfWorkScopeFactory>());
    }

    [Fact]
    public void AddUnitOfWork_ResolvesTheSameContextForUnitOfWorkAndRepository()
    {
        // The invariant everything else rests on: one DbContext per scope, shared. If this breaks,
        // SaveChanges stops covering the repository's writes and transactions stop containing them.
        ServiceCollection services = CreateServicesWithContext();
        services.AddUnitOfWork<WidgetDbContext>(uow => uow.AddRepository<IWidgetRepository, WidgetRepository>());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        var repository = (IDbContextBound)scope.ServiceProvider.GetRequiredService<IWidgetRepository>();
        var context = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();

        Assert.Same(context, repository.DbContext);
    }

    [Fact]
    public void AddUnitOfWork_WithSingletonContext_ThrowsExplainingTheLifetimeRequirement()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DbContextOptionsBuilder<WidgetDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);
        services.AddSingleton<WidgetDbContext>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddUnitOfWork<WidgetDbContext>());

        Assert.Contains("Singleton", exception.Message, StringComparison.Ordinal);
        Assert.Contains("scoped", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddUnitOfWork_BeforeAddDbContext_DoesNotThrow()
    {
        // Registration order should not be a trap. The lifetime check only fires on a context that is
        // already registered wrongly; a context registered afterwards is the container's problem to
        // report, and it does so clearly.
        var services = new ServiceCollection();

        services.AddUnitOfWork<WidgetDbContext>();
        services.AddDbContext<WidgetDbContext>(options => options.UseNpgsql(ConnectionString));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void AddUnitOfWork_CalledTwiceForOneContext_AccumulatesRepositories()
    {
        // Composition split across modules should add up rather than have the second call silently
        // discarded, which is what a fresh registration object would produce.
        ServiceCollection services = CreateServicesWithContext();

        services.AddUnitOfWork<WidgetDbContext>(uow => uow.AddRepository<IWidgetRepository, WidgetRepository>());
        services.AddUnitOfWork<WidgetDbContext>(uow => uow.AddRepository<IGadgetRepository, GadgetRepository>());

        using ServiceProvider provider = services.BuildServiceProvider();
        var registration = provider.GetRequiredService<UnitOfWorkRegistration<WidgetDbContext>>();

        Assert.Contains(typeof(IWidgetRepository), registration.RepositoryTypes);
        Assert.Contains(typeof(IGadgetRepository), registration.RepositoryTypes);
    }

    [Fact]
    public void AddUnitOfWork_WithSecondContext_KeepsTheFirstAsDefaultAndKeysTheSecond()
    {
        ServiceCollection services = CreateServicesWithContext();
        services.AddDbContext<GadgetDbContext>(options => options.UseNpgsql(ConnectionString));

        services.AddUnitOfWork<WidgetDbContext>();
        services.AddUnitOfWork<GadgetDbContext>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // The first context keeps the plain registration â€” adding a second context must not silently
        // repoint everything that already asks for IUnitOfWork.
        Assert.IsType<EfUnitOfWork<WidgetDbContext>>(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());

        // The second is still reachable, by key.
        Assert.IsType<EfUnitOfWork<GadgetDbContext>>(
            scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(GadgetDbContext)));
    }

    [Fact]
    public void AddUnitOfWork_WithCustomKey_RegistersUnderThatKey()
    {
        ServiceCollection services = CreateServicesWithContext();
        services.AddDbContext<GadgetDbContext>(options => options.UseNpgsql(ConnectionString));

        services.AddUnitOfWork<WidgetDbContext>();
        services.AddUnitOfWork<GadgetDbContext>(uow => uow.WithKey("gadgets"));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<EfUnitOfWork<GadgetDbContext>>(
            scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>("gadgets"));
    }

    [Fact]
    public void AddUnitOfWork_WithTwoContextsBothClaimingDefault_ThrowsRatherThanPickingOne()
    {
        ServiceCollection services = CreateServicesWithContext();
        services.AddDbContext<GadgetDbContext>(options => options.UseNpgsql(ConnectionString));

        services.AddUnitOfWork<WidgetDbContext>(uow => uow.AsDefault());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddUnitOfWork<GadgetDbContext>(uow => uow.AsDefault()));

        Assert.Contains("AsDefault", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddNpgsqlUnitOfWork_ClassifiesBySqlState()
    {
        ServiceCollection services = CreateServicesWithContext();
        services.AddNpgsqlUnitOfWork<WidgetDbContext>();

        Assert.IsType<DuplicateKeyException>(TranslateUniqueViolation(services));
    }

    [Fact]
    public void AddNpgsqlUnitOfWork_AfterAnEarlierAddUnitOfWork_StillClassifiesBySqlState()
    {
        // The regression. AddUnitOfWork appends the Entity Framework catch-all on every call, so any
        // earlier call — a second context, or this same context configured across two modules — put it
        // in front of the PostgreSQL translator. Duplicate keys then arrived as
        // UnclassifiedPersistenceException (no 409 to key on) and serialization failures stopped being
        // transient, so ExecuteInTransactionAsync quietly stopped retrying. Asserted on behaviour
        // rather than on the order of the registered list, because the list order is no longer what
        // decides it.
        ServiceCollection services = CreateServicesWithContext();
        services.AddDbContext<GadgetDbContext>(options => options.UseNpgsql(ConnectionString));

        services.AddUnitOfWork<GadgetDbContext>();
        services.AddNpgsqlUnitOfWork<WidgetDbContext>();

        Assert.IsType<DuplicateKeyException>(TranslateUniqueViolation(services));
    }

    [Fact]
    public void AddNpgsqlUnitOfWork_AfterAddUnitOfWorkForTheSameContext_StillClassifiesBySqlState()
    {
        // Same trap by the route the documentation actively recommends: composition split across
        // modules, one contributing repositories and one adding the provider.
        ServiceCollection services = CreateServicesWithContext();

        services.AddUnitOfWork<WidgetDbContext>(uow => uow.AddRepository<IWidgetRepository, WidgetRepository>());
        services.AddNpgsqlUnitOfWork<WidgetDbContext>();

        Assert.IsType<DuplicateKeyException>(TranslateUniqueViolation(services));
    }

    [Fact]
    public void AddUnitOfWork_WhenTheSameContextClaimsTheDefaultTwice_DoesNotThrow()
    {
        // Additive composition is documented as supported, so a module that contributes repositories
        // and a module that asserts the default must be able to coexist. This used to throw blaming
        // "another persistence context" — which was this same context.
        ServiceCollection services = CreateServicesWithContext();

        services.AddUnitOfWork<WidgetDbContext>(uow => uow.AddRepository<IWidgetRepository, WidgetRepository>());
        services.AddUnitOfWork<WidgetDbContext>(uow => uow.AsDefault());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<EfUnitOfWork<WidgetDbContext>>(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    [Fact]
    public void AddUnitOfWork_WhenTheSameContextRepeatsItsKey_DoesNotThrow()
    {
        ServiceCollection services = CreateServicesWithContext();

        services.AddUnitOfWork<WidgetDbContext>(uow => uow.WithKey("widgets"));
        services.AddUnitOfWork<WidgetDbContext>(uow => uow.WithKey("widgets"));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<EfUnitOfWork<WidgetDbContext>>(
            scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>("widgets"));
    }

    [Fact]
    public void AddUnitOfWork_WithTwoContextsClaimingOneKey_ThrowsRatherThanDiscardingTheSecond()
    {
        // TryAddKeyedScoped used to drop the second registration in silence, so everything asking for
        // this key resolved the first context — writes landing in the wrong schema, outside whatever
        // transaction the caller thought it had opened.
        ServiceCollection services = CreateServicesWithContext();
        services.AddDbContext<GadgetDbContext>(options => options.UseNpgsql(ConnectionString));

        services.AddUnitOfWork<WidgetDbContext>(uow => uow.WithKey("shared"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddUnitOfWork<GadgetDbContext>(uow => uow.WithKey("shared")));

        Assert.Contains("shared", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(WidgetDbContext), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddUnitOfWork_WithTwoContextsTakingTheirDefaultKeys_DoesNotCollide()
    {
        // The default key is typeof(TContext), so two contexts can never collide without asking to.
        ServiceCollection services = CreateServicesWithContext();
        services.AddDbContext<GadgetDbContext>(options => options.UseNpgsql(ConnectionString));

        services.AddUnitOfWork<WidgetDbContext>();
        services.AddUnitOfWork<GadgetDbContext>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<EfUnitOfWork<WidgetDbContext>>(
            scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(WidgetDbContext)));
        Assert.IsType<EfUnitOfWork<GadgetDbContext>>(
            scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(GadgetDbContext)));
    }

    private static PersistenceException? TranslateUniqueViolation(IServiceCollection services)
    {
        using ServiceProvider provider = services.BuildServiceProvider();

        var composite = provider.GetRequiredService<CompositePersistenceExceptionTranslator>();

        return composite.Translate(new DbUpdateException(
            "An error occurred while saving.",
            new PostgresException(
                messageText: "duplicate key value violates unique constraint",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UniqueViolation,
                constraintName: "ix_widgets_name")));
    }

    private static ServiceCollection CreateServicesWithContext()
    {
        var services = new ServiceCollection();
        services.AddDbContext<WidgetDbContext>(options => options.UseNpgsql(ConnectionString));

        return services;
    }
}
