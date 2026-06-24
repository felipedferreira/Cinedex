namespace Movies.Persistence.Postgres.Constants;

internal static class DatabaseConstants
{
    public const string CatalogSchema = "catalog";

    public static class Title
    {
        public const string PrimaryKey = "PK_titles";

        public static class Columns
        {
            public const string Title = "title";
            public const string TitleType = "titleType";
        }
    }

    public static class Genre
    {
        public const string PrimaryKey = "PK_genres";
        public const string NameIndex = "IX_genres_name";
    }

    public static class PostgresTypes
    {
        public const string UuidArray = "uuid[]";
    }
}
