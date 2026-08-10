using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <summary>
    /// The secondary round becomes "which card, announced by whom" instead of a bare open/closed flag (it now
    /// outlives the turn advance), and Politics gets a "the speaker has not been appointed yet" flag.
    ///
    /// EF's scaffolder offered to RENAME <c>SecondaryOpen</c> to <c>SpeakerPending</c> — both are booleans, so
    /// it looks like a rename to a schema differ. It isn't: an open secondary round would arrive as a pending
    /// speaker appointment and block the next turn end. Dropped and added instead.
    /// </summary>
    public partial class SecondaryRoundAndSpeakerPending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondaryOpen",
                table: "Sessions");

            migrationBuilder.AddColumn<int>(
                name: "SecondaryCardId",
                table: "Sessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecondaryOwnerId",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SpeakerPending",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondaryCardId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SecondaryOwnerId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SpeakerPending",
                table: "Sessions");

            migrationBuilder.AddColumn<bool>(
                name: "SecondaryOpen",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
