namespace Cinedex.WebService.Contracts.Enums;

public enum TitleType
{
    /// <summary>
    /// A feature-length or standalone movie.
    /// </summary>
    Movie = 0,

    /// <summary>
    /// A television series.
    /// </summary>
    TvSeries = 1,

    /// <summary>
    /// An individual television episode.
    /// </summary>
    TvEpisode = 2,

    /// <summary>
    /// A short-form title.
    /// </summary>
    Short = 3,

    /// <summary>
    /// A television special.
    /// </summary>
    TvSpecial = 4,

    /// <summary>
    /// A direct-to-video or web video title.
    /// </summary>
    Video = 5,
}