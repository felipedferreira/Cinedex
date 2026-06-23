using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Movies.Persistence.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddGenres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "genres",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genres", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movie_genres",
                columns: table => new
                {
                    genre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movie_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movie_genres", x => new { x.genre_id, x.movie_id });
                    table.ForeignKey(
                        name: "FK_movie_genres_genres_genre_id",
                        column: x => x.genre_id,
                        principalTable: "genres",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_movie_genres_movies_movie_id",
                        column: x => x.movie_id,
                        principalTable: "movies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "genres",
                columns: new[] { "id", "description", "name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "Action genre featuring physical conflict and adventure.", "Action" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "Comedy genre focused on humor and entertainment.", "Comedy" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "Drama genre focused on emotional storytelling.", "Drama" },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "Fantasy genre with magical and imaginative elements.", "Fantasy" },
                    { new Guid("11111111-1111-1111-1111-111111111105"), "Horror genre designed to evoke fear.", "Horror" },
                    { new Guid("11111111-1111-1111-1111-111111111106"), "Romance genre focused on relationships and love.", "Romance" },
                    { new Guid("11111111-1111-1111-1111-111111111107"), "Science fiction genre exploring futuristic or scientific concepts.", "SciFi" },
                    { new Guid("11111111-1111-1111-1111-111111111108"), "Thriller genre designed to create suspense.", "Thriller" },
                    { new Guid("11111111-1111-1111-1111-111111111109"), "Animation genre using animated characters and storytelling.", "Animation" },
                    { new Guid("11111111-1111-1111-1111-11111111110a"), "Adventure genre featuring exploration and quests.", "Adventure" },
                    { new Guid("11111111-1111-1111-1111-11111111110b"), "Crime genre focused on criminal activities and investigations.", "Crime" },
                    { new Guid("11111111-1111-1111-1111-11111111110c"), "Documentary genre presenting factual information.", "Documentary" },
                    { new Guid("11111111-1111-1111-1111-11111111110d"), "Family genre appropriate for all ages.", "Family" },
                    { new Guid("11111111-1111-1111-1111-11111111110e"), "History genre based on historical events.", "History" },
                    { new Guid("11111111-1111-1111-1111-11111111110f"), "Musical genre featuring songs and musical performances.", "Musical" },
                    { new Guid("11111111-1111-1111-1111-111111111110"), "Mystery genre focused on solving puzzles or crimes.", "Mystery" },
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Western genre set in the Old West.", "Western" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_genres_name",
                table: "genres",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_movie_genres_movie_id",
                table: "movie_genres",
                column: "movie_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movie_genres");

            migrationBuilder.DropTable(
                name: "genres");
        }
    }
}