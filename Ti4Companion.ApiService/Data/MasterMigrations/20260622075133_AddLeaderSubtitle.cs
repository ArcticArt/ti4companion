using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class AddLeaderSubtitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "Leaders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubtitleDe",
                table: "Leaders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // The old FlavorText/FlavorTextDe actually held the leader's epithet (e.g. "The Hungry
            // Shadow"), not the lore blurb. Move it into the new Subtitle/SubtitleDe columns and free
            // FlavorText/FlavorTextDe for the real lore text.
            migrationBuilder.Sql("UPDATE Leaders SET Subtitle = FlavorText, SubtitleDe = FlavorTextDe, FlavorText = '', FlavorTextDe = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Leaders SET FlavorText = Subtitle, FlavorTextDe = SubtitleDe;");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "Leaders");

            migrationBuilder.DropColumn(
                name: "SubtitleDe",
                table: "Leaders");
        }
    }
}
