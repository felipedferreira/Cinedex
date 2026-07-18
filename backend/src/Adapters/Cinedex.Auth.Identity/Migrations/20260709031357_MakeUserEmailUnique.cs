using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinedex.Auth.Identity.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserEmailUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM auth."AspNetUsers"
                        WHERE "normalizedEmail" IS NOT NULL
                        GROUP BY "normalizedEmail"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot create unique EmailIndex because duplicate normalizedEmail values exist in auth.AspNetUsers.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "auth",
                table: "AspNetUsers",
                column: "normalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                schema: "auth",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "auth",
                table: "AspNetUsers",
                column: "normalizedEmail");
        }
    }
}
