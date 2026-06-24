using Movies.Domain.Enums;

namespace Movies.Application.Titles.UpdateTitle;

public sealed record UpdateTitleCommand(
    Guid Id,
    string Title,
    TitleType Type,
    int YearOfRelease,
    string? Description,
    IReadOnlyList<Guid> GenreIds);