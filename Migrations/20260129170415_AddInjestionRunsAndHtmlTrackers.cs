using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitMagnetRssImporter.Migrations
{
    /// <inheritdoc />
    public partial class AddInjestionRunsAndHtmlTrackers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 UPDATE RssFeeds
                                 SET LastRunId = NULL
                                 WHERE LastRunId IS NOT NULL;
                                 """);

            migrationBuilder.DropForeignKey(
                name: "FK_RssFeeds_RssFeedRuns_LastRunId",
                table: "RssFeeds");

            migrationBuilder.CreateTable(
                name: "HtmlSeenItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrackerId = table.Column<long>(type: "INTEGER", nullable: false),
                    ItemKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    InfoHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HtmlSeenItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngestionRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceType = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceId = table.Column<long>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Phase = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: true),
                    HeartbeatAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    HttpStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemsParsed = table.Column<int>(type: "INTEGER", nullable: false),
                    Candidates = table.Column<int>(type: "INTEGER", nullable: false),
                    NewItems = table.Column<int>(type: "INTEGER", nullable: false),
                    Imported = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedNoInfoHash = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedSeen = table.Column<int>(type: "INTEGER", nullable: false),
                    PagesVisited = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemsScanned = table.Column<int>(type: "INTEGER", nullable: false),
                    SeenStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HtmlTrackers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    StartUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    PollIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPagesPerRun = table.Column<int>(type: "INTEGER", nullable: false),
                    StopAfterSeenStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    RowSelector = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    DetailLinkSelector = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    TitleSelector = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    NextPageSelector = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    InfoHashRegex = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastRunId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HtmlTrackers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HtmlTrackers_IngestionRuns_LastRunId",
                        column: x => x.LastRunId,
                        principalTable: "IngestionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HtmlSeenItems_TrackerId_ItemKey",
                table: "HtmlSeenItems",
                columns: new[] { "TrackerId", "ItemKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HtmlTrackers_LastRunId",
                table: "HtmlTrackers",
                column: "LastRunId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionRuns_SourceType_SourceId_IsActive",
                table: "IngestionRuns",
                columns: new[] { "SourceType", "SourceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionRuns_SourceType_SourceId_StartedAt",
                table: "IngestionRuns",
                columns: new[] { "SourceType", "SourceId", "StartedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_RssFeeds_IngestionRuns_LastRunId",
                table: "RssFeeds",
                column: "LastRunId",
                principalTable: "IngestionRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RssFeeds_IngestionRuns_LastRunId",
                table: "RssFeeds");

            migrationBuilder.DropTable(
                name: "HtmlSeenItems");

            migrationBuilder.DropTable(
                name: "HtmlTrackers");

            migrationBuilder.DropTable(
                name: "IngestionRuns");

            migrationBuilder.AddForeignKey(
                name: "FK_RssFeeds_RssFeedRuns_LastRunId",
                table: "RssFeeds",
                column: "LastRunId",
                principalTable: "RssFeedRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
