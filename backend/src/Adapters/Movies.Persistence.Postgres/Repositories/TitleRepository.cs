using Microsoft.EntityFrameworkCore;
using Movies.Application.Abstractions;
using Movies.Domain.TitleAggregate;

namespace Movies.Persistence.Postgres.Repositories;

internal sealed class TitleRepository(FilmDbContext dbContext) : ITitleRepository
{
    public async Task<IReadOnlyList<Title>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Titles.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Title?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Titles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<Title> CreateAsync(Title title, CancellationToken cancellationToken)
    {
        dbContext.Titles.Add(title);
        await dbContext.SaveChangesAsync(cancellationToken);

        return title;
    }

    public async Task<bool> UpdateAsync(Title title, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Titles
            .FirstOrDefaultAsync(m => m.Id == title.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        existing.Name = title.Name;
        existing.Type = title.Type;
        existing.YearOfRelease = title.YearOfRelease;
        existing.Description = title.Description;
        existing.GenreIds = title.GenreIds;

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.Titles
            .Where(m => m.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return rowsAffected > 0;
    }
}