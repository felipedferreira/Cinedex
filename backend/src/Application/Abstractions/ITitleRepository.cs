using Cinedex.Domain.TitleAggregate;

namespace Cinedex.Application.Abstractions;

public interface ITitleRepository
{
    Task<IReadOnlyList<Title>> GetAllAsync(CancellationToken cancellationToken);

    Task<Title?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task CreateAsync(Title title, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Title title, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}