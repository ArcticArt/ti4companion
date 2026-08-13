using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShowJoinQr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowJoinQr",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Existing rows default to "off", which is right for a running match — but a session still in
            // SETUP (phase 0) showed the QR until now, and taking it away mid-setup would look like a bug.
            migrationBuilder.Sql("UPDATE Sessions SET ShowJoinQr = 1 WHERE Phase = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowJoinQr",
                table: "Sessions");
        }
    }
}
