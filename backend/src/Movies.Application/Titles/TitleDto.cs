using Movies.Domain.Enums;

namespace Movies.Application.Titles;

public sealed record TitleDto(Guid Id, string Title, TitleType Type, int YearOfRelease, string? Description);