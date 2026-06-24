using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Movies.Persistence.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "genres",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genres", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "titles",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    titleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    yearOfRelease = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    genreIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_titles", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "genres",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { new Guid("00482a38-516a-4e2b-a4c6-f0f6e6259d2a"), "Documentary" },
                    { new Guid("03875139-b79e-4a8d-9053-3d937c8ae165"), "Family" },
                    { new Guid("3687d461-e5cb-447a-81f4-c82716f4feb1"), "Adventure" },
                    { new Guid("49ef5cc8-f94a-4066-ba86-0b27aa9bd642"), "Musical" },
                    { new Guid("50267304-8ba6-495b-9ef4-806d2dff1656"), "History" },
                    { new Guid("535c3954-aa92-4b97-8a75-92cd5ef6f8c6"), "Thriller" },
                    { new Guid("65515a68-1b05-4d8b-94e0-5599d671a643"), "Horror" },
                    { new Guid("655b1256-859a-4c9f-bed2-31ac669d0f18"), "SciFi" },
                    { new Guid("6deddeee-0f11-4d07-a7c2-b70229ab58de"), "Drama" },
                    { new Guid("80acabbb-c0f8-428e-bd12-23187789928f"), "Romance" },
                    { new Guid("90837e33-57a3-4793-a07b-5c47a7daeff8"), "Fantasy" },
                    { new Guid("9d6c31eb-2c02-4a48-b4dc-24767675f82a"), "Action" },
                    { new Guid("aef33796-2504-403c-a2ac-99ff7ce966e8"), "Crime" },
                    { new Guid("bba55f7e-81c0-4f11-a9c9-057f5ee74a05"), "Mystery" },
                    { new Guid("c469256f-6f96-4602-b546-82be46807a6f"), "Western" },
                    { new Guid("d7a0f348-959f-416b-8d50-8e35a0bdbb26"), "Comedy" },
                    { new Guid("f271eb24-ef49-4cfb-af1c-edf8c45a4a37"), "Animation" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_genres_name",
                schema: "catalog",
                table: "genres",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "genres",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "titles",
                schema: "catalog");
        }
    }
}
