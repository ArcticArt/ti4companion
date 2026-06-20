using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionPaused : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Paused",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Paused",
                table: "Sessions");
        }
    }
}
