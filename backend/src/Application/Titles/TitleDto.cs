using Cinedex.Domain.Enums;

namespace Cinedex.Application.Titles;

public sealed record TitleDto(Guid Id, string Title, TitleType Type, int YearOfRelease, string? Description);