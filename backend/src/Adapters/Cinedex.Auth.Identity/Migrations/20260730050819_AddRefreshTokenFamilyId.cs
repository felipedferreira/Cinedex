using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinedex.Auth.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenFamilyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows written before this column existed belong to no real family, and any value
            // invented for them would be a fiction that a future family-wide revocation would act
            // on — reporting a contained compromise while containing nothing. Refresh tokens are
            // ephemeral by design (seven-day maximum) and losing one costs a single login, so every
            // stored token is invalidated instead of backfilled. Sessions that were live when this
            // ran end; clients log in again.
            //
            // Deleting rather than aborting on existing data is also deliberate. The Compose
            // migrator gates the web service on its exit code, so a migration that could reject a
            // populated table would stop the whole stack from starting against an existing volume.
            // A DELETE cannot fail on data.
            migrationBuilder.Sql(
                """
                DELETE FROM auth."refreshTokens";
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "familyId",
                schema: "auth",
                table: "refreshTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // The zero-Guid default above exists only so ADD COLUMN NOT NULL can never fail, even if
            // a row were written between the delete and this statement. The application is the sole
            // authority on family membership, so the default is dropped immediately: left in place it
            // would let some later familyId-less insert land silently in one shared family, and a
            // family-wide revocation would then reach every token that ever defaulted.
            migrationBuilder.Sql(
                """
                ALTER TABLE auth."refreshTokens" ALTER COLUMN "familyId" DROP DEFAULT;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_refreshTokens_familyId",
                schema: "auth",
                table: "refreshTokens",
                column: "familyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately not symmetric: the refresh tokens Up() invalidated cannot be restored.
            // Reverting removes the column and its index; users log in again.
            migrationBuilder.DropIndex(
                name: "IX_refreshTokens_familyId",
                schema: "auth",
                table: "refreshTokens");

            migrationBuilder.DropColumn(
                name: "familyId",
                schema: "auth",
                table: "refreshTokens");
        }
    }
}
