using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Movies.Persistence.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class MovieGenresByIdArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movie_genres");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "genre_ids",
                table: "movies",
                type: "uuid[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "genre_ids",
                table: "movies");

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

            migrationBuilder.CreateIndex(
                name: "IX_movie_genres_movie_id",
                table: "movie_genres",
                column: "movie_id");
        }
    }
}
