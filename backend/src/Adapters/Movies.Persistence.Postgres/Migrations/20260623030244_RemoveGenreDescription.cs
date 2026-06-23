using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Movies.Persistence.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGenreDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "genres");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "genres",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                column: "description",
                value: "Action genre featuring physical conflict and adventure.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                column: "description",
                value: "Comedy genre focused on humor and entertainment.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                column: "description",
                value: "Drama genre focused on emotional storytelling.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                column: "description",
                value: "Fantasy genre with magical and imaginative elements.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                column: "description",
                value: "Horror genre designed to evoke fear.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                column: "description",
                value: "Romance genre focused on relationships and love.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                column: "description",
                value: "Science fiction genre exploring futuristic or scientific concepts.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                column: "description",
                value: "Thriller genre designed to create suspense.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                column: "description",
                value: "Animation genre using animated characters and storytelling.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-11111111110a"),
                column: "description",
                value: "Adventure genre featuring exploration and quests.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-11111111110b"),
                column: "description",
                value: "Crime genre focused on criminal activities and investigations.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-11111111110c"),
                column: "description",
                value: "Documentary genre presenting factual information.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-11111111110d"),
                column: "description",
                value: "Family genre appropriate for all ages.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-11111111110e"),
                column: "description",
                value: "History genre based on historical events.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-11111111110f"),
                column: "description",
                value: "Musical genre featuring songs and musical performances.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"),
                column: "description",
                value: "Mystery genre focused on solving puzzles or crimes.");

            migrationBuilder.UpdateData(
                table: "genres",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "description",
                value: "Western genre set in the Old West.");
        }
    }
}