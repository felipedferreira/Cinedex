using Cinedex.Application.Titles;
using Cinedex.Application.Titles.CreateTitle;
using Cinedex.Application.Titles.UpdateTitle;
using Cinedex.WebService.Contracts.Requests;
using Cinedex.WebService.Contracts.Responses;
using Cinedex.WebService.Endpoints.Genres;
using ContractTitleType = Cinedex.WebService.Contracts.Enums.TitleType;
using DomainTitleType = Cinedex.Domain.Enums.TitleType;

namespace Cinedex.WebService.Endpoints.Titles;

internal static class TitleMappings
{
    public static CreateTitleCommand ToCommand(this CreateTitlesRequest request) =>
        new(request.Title, ToDomain(request.Type), request.YearOfRelease, request.Description, (request.GenreIds ?? []).ToList());

    public static UpdateTitleCommand ToCommand(this UpdateTitlesRequest request, Guid id) =>
        new(id, request.Title, ToDomain(request.Type), request.YearOfRelease, request.Description, (request.GenreIds ?? []).ToList());

    public static TitleResponse ToResponse(this TitleDto title) => new()
    {
        Id = title.Id,
        Title = title.Title,
        Type = ToContract(title.Type),
        YearOfRelease = title.YearOfRelease,
        Description = title.Description,
    };

    public static TitleDetailsResponse ToResponse(this TitleDetailsDto title) => new()
    {
        Id = title.Id,
        Title = title.Title,
        Type = ToContract(title.Type),
        YearOfRelease = title.YearOfRelease,
        Description = title.Description,
        Genres = title.Genres.Select(genre => genre.ToResponse()).ToList(),
    };

    public static TitlesResponse ToResponse(this IEnumerable<TitleDto> titles) => new()
    {
        Titles = titles.Select(title => title.ToResponse()),
    };

    private static DomainTitleType ToDomain(ContractTitleType type) => type switch
    {
        ContractTitleType.Movie => DomainTitleType.Movie,
        ContractTitleType.TvSeries => DomainTitleType.TvSeries,
        ContractTitleType.TvEpisode => DomainTitleType.TvEpisode,
        ContractTitleType.Short => DomainTitleType.Short,
        ContractTitleType.TvSpecial => DomainTitleType.TvSpecial,
        ContractTitleType.Video => DomainTitleType.Video,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    private static ContractTitleType ToContract(DomainTitleType type) => type switch
    {
        DomainTitleType.Movie => ContractTitleType.Movie,
        DomainTitleType.TvSeries => ContractTitleType.TvSeries,
        DomainTitleType.TvEpisode => ContractTitleType.TvEpisode,
        DomainTitleType.Short => ContractTitleType.Short,
        DomainTitleType.TvSpecial => ContractTitleType.TvSpecial,
        DomainTitleType.Video => ContractTitleType.Video,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}