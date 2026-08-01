using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinedex.Auth.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenRetentionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_refreshTokens_revokedAtUtc_expiresAtUtc",
                schema: "auth",
                table: "refreshTokens",
                columns: new[] { "revokedAtUtc", "expiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refreshTokens_revokedAtUtc_expiresAtUtc",
                schema: "auth",
                table: "refreshTokens");
        }
    }
}
