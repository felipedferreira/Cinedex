namespace Cinedex.Persistence.Postgres.Entities;

internal sealed class TitleGenre
{
    public required Guid TitleId { get; init; }

    public required Guid GenreId { get; init; }
}