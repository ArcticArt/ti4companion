using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgendaVoteLocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Locked",
                table: "AgendaVotes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Locked",
                table: "AgendaVotes");
        }
    }
}
