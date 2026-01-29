using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitMagnetRssImporter.Migrations
{
    /// <inheritdoc />
    public partial class AddRssFeedRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastRunId",
                table: "RssFeeds",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RssFeedRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeedId = table.Column<long>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    HttpStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemsParsed = table.Column<int>(type: "INTEGER", nullable: false),
                    Candidates = table.Column<int>(type: "INTEGER", nullable: false),
                    NewItems = table.Column<int>(type: "INTEGER", nullable: false),
                    Imported = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedNoInfoHash = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedSeen = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RssFeedRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RssFeedRuns_RssFeeds_FeedId",
                        column: x => x.FeedId,
                        principalTable: "RssFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RssFeeds_LastRunId",
                table: "RssFeeds",
                column: "LastRunId");

            migrationBuilder.CreateIndex(
                name: "IX_RssFeedRuns_FeedId_StartedAt",
                table: "RssFeedRuns",
                columns: new[] { "FeedId", "StartedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_RssFeeds_RssFeedRuns_LastRunId",
                table: "RssFeeds",
                column: "LastRunId",
                principalTable: "RssFeedRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RssFeeds_RssFeedRuns_LastRunId",
                table: "RssFeeds");

            migrationBuilder.DropTable(
                name: "RssFeedRuns");

            migrationBuilder.DropIndex(
                name: "IX_RssFeeds_LastRunId",
                table: "RssFeeds");

            migrationBuilder.DropColumn(
                name: "LastRunId",
                table: "RssFeeds");
        }
    }
}
