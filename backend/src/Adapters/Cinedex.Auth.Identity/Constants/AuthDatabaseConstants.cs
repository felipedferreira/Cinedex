namespace Cinedex.Auth.Identity.Constants;

internal static class AuthDatabaseConstants
{
    public const string AuthSchema = "auth";

    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    // The read-write connection, shared with the film adapter — one database, two schemas.
    public const string DefaultConnectionName = "DefaultConnection";

    // Optional. Point it at a role holding only SELECT to get the privilege split; leave it unset and
    // reads fall back to DefaultConnection, which is what every current deployment does.
    public const string ReadOnlyConnectionName = "ReadOnlyConnection";

    public static class RefreshToken
    {
        public const string Table = "refreshTokens";
        public const string PrimaryKey = "PK_refreshTokens";
        public const string TokenHashIndex = "IX_refreshTokens_tokenHash";
        public const string UserIdIndex = "IX_refreshTokens_userId";

        public const string FamilyIdIndex = "IX_refreshTokens_familyId";

        public const string RetentionIndex = "IX_refreshTokens_revokedAtUtc_expiresAtUtc";
    }
}