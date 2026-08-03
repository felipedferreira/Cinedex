using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.EntityFrameworkCore.DependencyInjection;

/// <summary>
/// The record of what was registered for one persistence context: which repositories the unit of work
/// will hand out, and how strictly it checks them.
/// </summary>
/// <remarks>
/// Built by <c>AddUnitOfWork</c> and registered as a singleton. You do not construct one; it is a
/// constructor dependency of <see cref="EfUnitOfWork{TContext}"/> and is documented here because it
/// appears in that signature.
/// </remarks>
/// <typeparam name="TContext">The persistence context this registration describes.</typeparam>
public sealed class UnitOfWorkRegistration<TContext>
    where TContext : DbContext
{
    private readonly HashSet<Type> _repositoryTypes = [];

    internal UnitOfWorkRegistration()
    {
    }

    /// <summary>
    /// Gets a value indicating whether the unit of work verifies that each repository it resolves is
    /// bound to its own persistence context.
    /// </summary>
    /// <remarks>
    /// On by default. The check costs one reference comparison the first time a given repository type
    /// is resolved in a scope, and it catches a class of bug — writes quietly landing outside the
    /// transaction meant to contain them — that otherwise shows up as data inconsistency long after
    /// the code that caused it shipped. See <see cref="IDbContextBound"/>.
    /// </remarks>
    public bool ValidateRepositoryContext { get; internal set; } = true;

    /// <summary>
    /// Gets the repository interfaces registered with this unit of work.
    /// </summary>
    /// <remarks>
    /// Used to tell "you never registered this repository" apart from "you registered it against a
    /// different persistence context", which are the two ways
    /// <see cref="Abstractions.IUnitOfWork.Repository{TRepository}"/> fails and which have completely
    /// different fixes.
    /// </remarks>
    public IReadOnlyCollection<Type> RepositoryTypes => _repositoryTypes;

    internal void AddRepositoryType(Type repositoryType) => _repositoryTypes.Add(repositoryType);
}
