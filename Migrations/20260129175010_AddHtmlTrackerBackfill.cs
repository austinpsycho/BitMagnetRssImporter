using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitMagnetRssImporter.Migrations
{
    /// <inheritdoc />
    public partial class AddHtmlTrackerBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BackfillCompletedAt",
                table: "HtmlTrackers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BackfillEnabled",
                table: "HtmlTrackers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BackfillNextUrl",
                table: "HtmlTrackers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackfillCompletedAt",
                table: "HtmlTrackers");

            migrationBuilder.DropColumn(
                name: "BackfillEnabled",
                table: "HtmlTrackers");

            migrationBuilder.DropColumn(
                name: "BackfillNextUrl",
                table: "HtmlTrackers");
        }
    }
}
