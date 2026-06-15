using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayerIsHost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHost",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Backfill: for sessions created before this flag existed, the lowest-seat player is the host.
            migrationBuilder.Sql(@"
                UPDATE ""Players"" p SET ""IsHost"" = true
                WHERE p.""SeatOrder"" = (
                    SELECT MIN(p2.""SeatOrder"") FROM ""Players"" p2 WHERE p2.""SessionId"" = p.""SessionId"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHost",
                table: "Players");
        }
    }
}
