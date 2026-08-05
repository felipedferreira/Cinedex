namespace FoundryOceanus.Persistence.EntityFrameworkCore.DependencyInjection;

/// <summary>
/// Startup-only record of which persistence context owns the unkeyed
/// <see cref="Abstractions.IUnitOfWork"/> registration and which service keys are already taken.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a singleton instance so that repeated <c>AddUnitOfWork</c> calls on the same service
/// collection can see what earlier ones did. Nothing resolves it — it exists to be read back off the
/// <c>IServiceCollection</c> during composition, the same trick
/// <see cref="UnitOfWorkRegistration{TContext}"/> uses.
/// </para>
/// <para>
/// The service collection cannot answer these questions on its own. Both registrations are factory
/// delegates over <c>EfUnitOfWork&lt;TContext&gt;</c>, and a delegate does not say which context it
/// closes over — so "is this key taken?" was previously answerable only as "does a descriptor exist?",
/// which cannot tell a second module configuring the same context apart from a different context
/// quietly stealing its key.
/// </para>
/// </remarks>
internal sealed class UnitOfWorkClaims
{
    private readonly Dictionary<object, Type> _keyOwners = [];

    /// <summary>
    /// Gets the context that claimed the unkeyed registration, or <see langword="null"/> if this
    /// library has not claimed it.
    /// </summary>
    public Type? DefaultOwner { get; private set; }

    public void ClaimDefault(Type contextType) => DefaultOwner = contextType;

    public Type? FindKeyOwner(object key) => _keyOwners.GetValueOrDefault(key);

    public void ClaimKey(object key, Type contextType) => _keyOwners[key] = contextType;
}
