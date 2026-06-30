using Cinedex.Application.Exceptions;
using Cinedex.Domain.GenreAggregate;

namespace Cinedex.Application.Titles;

// Helpers for linking a title to existing genres when handling create/update commands.
internal static class GenreLinking
{
    // Throws EntityNotFoundException if any requested genre id has no matching genre.
    public static void EnsureAllExist(IReadOnlyList<Guid> requestedIds, IReadOnlyList<Genre> found)
    {
        if (found.Count == requestedIds.Distinct().Count())
        {
            return;
        }

        var foundIds = found.Select(genre => genre.Id).ToHashSet();
        var missingId = requestedIds.First(id => !foundIds.Contains(id));

        throw new EntityNotFoundException(nameof(Genre), missingId);
    }
}