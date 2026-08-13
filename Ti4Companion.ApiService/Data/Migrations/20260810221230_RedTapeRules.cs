using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <summary>
    /// The Red Tape variants' rules become real (see <c>Services.RedTape</c>): an objective can be PURGED
    /// (Lite, once five Stage I are clear — it can never be scored afterwards), and the session remembers the
    /// round in which Lite's random removal already happened, so the two places the status phase can end do it
    /// exactly once. Both additive, both default to "nothing has happened yet".
    /// </summary>
    public partial class RedTapeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RedTapeRandomRound",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Purged",
                table: "SessionObjectives",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedTapeRandomRound",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Purged",
                table: "SessionObjectives");
        }
    }
}
