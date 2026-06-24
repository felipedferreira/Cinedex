using Movies.Domain.Enums;

namespace Movies.Application.Titles.CreateTitle;

public sealed record CreateTitleCommand(
    string Title,
    TitleType Type,
    int YearOfRelease,
    string? Description,
    IReadOnlyList<Guid> GenreIds);