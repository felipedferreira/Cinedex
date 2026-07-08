namespace Cinedex.Auth.Identity.Constants;

internal static class AuthDatabaseConstants
{
    public const string AuthSchema = "auth";

    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public static class RefreshToken
    {
        public const string Table = "refreshTokens";
        public const string PrimaryKey = "PK_refreshTokens";
        public const string TokenHashIndex = "IX_refreshTokens_tokenHash";
        public const string UserIdIndex = "IX_refreshTokens_userId";
    }
}
