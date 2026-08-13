using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrackSecondaryAbilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TrackSecondaryAbilities",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // The secondary round used to come with the turn timer and nothing else. Now that it has its own
            // option, a table already playing with a timer would silently lose it — so switch it on exactly
            // where it was already in effect. New sessions start with it off (opt-in), which is what the
            // column default handles.
            migrationBuilder.Sql(
                "UPDATE Sessions SET TrackSecondaryAbilities = 1 WHERE TurnTimerSeconds > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackSecondaryAbilities",
                table: "Sessions");
        }
    }
}
