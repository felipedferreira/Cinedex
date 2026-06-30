namespace Cinedex.Persistence.Postgres.Constants;

internal static class DatabaseConstants
{
    public const string CatalogSchema = "catalog";

    public static class Title
    {
        public const string PrimaryKey = "PK_titles";
    }

    public static class Genre
    {
        public const string PrimaryKey = "PK_genres";
        public const string NameIndex = "IX_genres_name";
    }

    public static class TitleGenre
    {
        public const string Table = "title_genres";
        public const string PrimaryKey = "PK_title_genres";
        public const string GenreIndex = "IX_title_genres_genreId";
    }
}