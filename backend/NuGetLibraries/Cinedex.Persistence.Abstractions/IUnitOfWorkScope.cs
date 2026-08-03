namespace Cinedex.Persistence.Abstractions;

/// <summary>
/// An explicitly created unit-of-work lifetime, for code that has no ambient scope of its own.
/// </summary>
/// <remarks>
/// Disposing the scope disposes the unit of work and the persistence context behind it. The
/// <see cref="UnitOfWork"/> must not be used afterwards, and neither must any repository resolved
/// from it.
/// </remarks>
public interface IUnitOfWorkScope : IAsyncDisposable
{
    /// <summary>
    /// Gets the unit of work belonging to this scope.
    /// </summary>
    IUnitOfWork UnitOfWork { get; }
}
