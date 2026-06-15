using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class DisplayModeHiddenVotesCustomObjectives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AgendaVotesHidden",
                table: "Sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DisplayMode",
                table: "Sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CustomName",
                table: "SessionObjectives",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomPoints",
                table: "SessionObjectives",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgendaVotesHidden",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "DisplayMode",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "CustomName",
                table: "SessionObjectives");

            migrationBuilder.DropColumn(
                name: "CustomPoints",
                table: "SessionObjectives");
        }
    }
}
