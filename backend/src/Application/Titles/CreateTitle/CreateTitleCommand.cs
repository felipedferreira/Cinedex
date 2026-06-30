using Cinedex.Domain.Enums;

namespace Cinedex.Application.Titles.CreateTitle;

public sealed record CreateTitleCommand(
    string Title,
    TitleType Type,
    int YearOfRelease,
    string? Description,
    IReadOnlyList<Guid> GenreIds);