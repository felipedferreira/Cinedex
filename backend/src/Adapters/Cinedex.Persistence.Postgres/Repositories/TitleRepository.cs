using Cinedex.Application.Abstractions;
using Cinedex.Domain.TitleAggregate;
using Cinedex.Persistence.Postgres.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinedex.Persistence.Postgres.Repositories;

internal sealed class TitleRepository(FilmDbContext dbContext) : ITitleRepository
{
    public async Task<IReadOnlyList<Title>> GetAllAsync(CancellationToken cancellationToken)
    {
        var titles = await dbContext.Titles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var titleIds = titles.Select(title => title.Id).ToList();
        var genreLinks = await dbContext.TitleGenres
            .AsNoTracking()
            .Where(titleGenre => titleIds.Contains(titleGenre.TitleId))
            .ToListAsync(cancellationToken);

        return titles
            .Select(title => HydrateGenreIds(
                title,
                genreLinks
                    .Where(titleGenre => titleGenre.TitleId == title.Id)
                    .Select(titleGenre => titleGenre.GenreId)))
            .ToList();
    }

    public async Task<Title?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var title = await dbContext.Titles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (title is null)
        {
            return null;
        }

        var genreIds = await dbContext.TitleGenres
            .AsNoTracking()
            .Where(titleGenre => titleGenre.TitleId == title.Id)
            .Select(titleGenre => titleGenre.GenreId)
            .ToListAsync(cancellationToken);

        return HydrateGenreIds(title, genreIds);
    }

    public async Task CreateAsync(Title title, CancellationToken cancellationToken)
    {
        dbContext.Titles.Add(title);
        dbContext.TitleGenres.AddRange(ToTitleGenres(title));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Title title, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Titles
            .FirstOrDefaultAsync(m => m.Id == title.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        existing.Name = title.Name;
        existing.Type = title.Type;
        existing.YearOfRelease = title.YearOfRelease;
        existing.Description = title.Description;

        await dbContext.TitleGenres
            .Where(titleGenre => titleGenre.TitleId == title.Id)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.TitleGenres.AddRange(ToTitleGenres(title));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.Titles
            .Where(m => m.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return rowsAffected > 0;
    }

    private static Title HydrateGenreIds(Title title, IEnumerable<Guid> genreIds)
    {
        title.ReplaceGenres(genreIds);
        return title;
    }

    private static IEnumerable<TitleGenre> ToTitleGenres(Title title)
    {
        return title.GenreIds.Select(genreId => new TitleGenre
        {
            TitleId = title.Id,
            GenreId = genreId,
        });
    }
}