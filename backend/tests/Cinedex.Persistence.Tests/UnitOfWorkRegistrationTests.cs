using Cinedex.Persistence.Tests.Fakes;
using FoundryOceanus.Persistence.Abstractions;
using FoundryOceanus.Persistence.EntityFrameworkCore;
using FoundryOceanus.Persistence.EntityFrameworkCore.DependencyInjection;
using FoundryOceanus.Persistence.EntityFrameworkCore.Postgres;
using FoundryOceanus.Persistence.EntityFrameworkCore.Postgres.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cinedex.Persistence.Tests;

/// <summary>
/// Registration-time behaviour. None of these touch a database — they assert on the shape of the
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

        // The first context keeps the plain registration — adding a second context must not silently
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
    public void AddNpgsqlUnitOfWork_PutsTheProviderTranslatorAheadOfTheCatchAll()
    {
        // Ordering is not cosmetic: reversed, the Entity Framework catch-all claims every
        // DbUpdateException and no SQLSTATE is ever read. See CompositePersistenceExceptionTranslatorTests.
        ServiceCollection services = CreateServicesWithContext();
        services.AddNpgsqlUnitOfWork<WidgetDbContext>();

        using ServiceProvider provider = services.BuildServiceProvider();
        List<IPersistenceExceptionTranslator> translators =
            [.. provider.GetServices<IPersistenceExceptionTranslator>()];

        int providerIndex = translators.FindIndex(translator => translator is NpgsqlExceptionTranslator);
        int catchAllIndex = translators.FindIndex(translator => translator is EntityFrameworkExceptionTranslator);

        Assert.True(providerIndex >= 0, "The PostgreSQL translator should be registered.");
        Assert.True(catchAllIndex >= 0, "The Entity Framework translator should be registered.");
        Assert.True(providerIndex < catchAllIndex, "The PostgreSQL translator must be consulted first.");
    }

    private static ServiceCollection CreateServicesWithContext()
    {
        var services = new ServiceCollection();
        services.AddDbContext<WidgetDbContext>(options => options.UseNpgsql(ConnectionString));

        return services;
    }
}
