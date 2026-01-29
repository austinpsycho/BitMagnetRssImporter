using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitMagnetRssImporter.Migrations
{
    /// <inheritdoc />
    public partial class RemovePerRssServerUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BitmagnetImportUrl",
                table: "RssFeeds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BitmagnetImportUrl",
                table: "RssFeeds",
                type: "TEXT",
                nullable: true);
        }
    }
}
