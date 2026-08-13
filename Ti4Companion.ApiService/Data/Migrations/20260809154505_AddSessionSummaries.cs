using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionSummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JoinCode = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastActivityUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PausedSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundsReached = table.Column<int>(type: "INTEGER", nullable: false),
                    EndPhase = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ObjectivesRevealed = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveExpansions = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultLanguage = table.Column<int>(type: "INTEGER", nullable: false),
                    TurnTimerSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    StrategyCardsPerPlayer = table.Column<int>(type: "INTEGER", nullable: false),
                    RedTapeLite = table.Column<bool>(type: "INTEGER", nullable: false),
                    WinnerName = table.Column<string>(type: "TEXT", nullable: true),
                    WinnerFactionId = table.Column<string>(type: "TEXT", nullable: true),
                    TopPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionSummaryPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionSummaryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FactionId = table.Column<string>(type: "TEXT", nullable: true),
                    ColorHex = table.Column<string>(type: "TEXT", nullable: false),
                    SeatOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    TechnologyCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionSummaryPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionSummaryPlayers_SessionSummaries_SessionSummaryId",
                        column: x => x.SessionSummaryId,
                        principalTable: "SessionSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionSummaries_CreatedAtUtc",
                table: "SessionSummaries",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SessionSummaries_SessionId",
                table: "SessionSummaries",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionSummaryPlayers_SessionSummaryId",
                table: "SessionSummaryPlayers",
                column: "SessionSummaryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionSummaryPlayers");

            migrationBuilder.DropTable(
                name: "SessionSummaries");
        }
    }
}
