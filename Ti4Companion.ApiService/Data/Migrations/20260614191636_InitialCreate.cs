using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agendas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameDe = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Elect = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    TextDe = table.Column<string>(type: "text", nullable: false),
                    Expansion = table.Column<int>(type: "integer", nullable: false),
                    RemovedInPok = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Factions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameDe = table.Column<string>(type: "text", nullable: false),
                    Expansion = table.Column<int>(type: "integer", nullable: false),
                    ColorHex = table.Column<string>(type: "text", nullable: false),
                    InitiativeOverride = table.Column<int>(type: "integer", nullable: true),
                    IconPath = table.Column<string>(type: "text", nullable: true),
                    StartingTechnologies = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Objectives",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameDe = table.Column<string>(type: "text", nullable: false),
                    Requirement = table.Column<string>(type: "text", nullable: false),
                    RequirementDe = table.Column<string>(type: "text", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Expansion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objectives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActivityUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DefaultLanguage = table.Column<int>(type: "integer", nullable: false),
                    ActiveExpansions = table.Column<int>(type: "integer", nullable: false),
                    CurrentRound = table.Column<int>(type: "integer", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    SpeakerPlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivePlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveStrategyCardId = table.Column<int>(type: "integer", nullable: true),
                    CurrentAgendaId = table.Column<string>(type: "text", nullable: true),
                    AllowEditAllPlayers = table.Column<bool>(type: "boolean", nullable: false),
                    RetentionHours = table.Column<int>(type: "integer", nullable: false),
                    ShowTechOverview = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategyCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameDe = table.Column<string>(type: "text", nullable: false),
                    Initiative = table.Column<int>(type: "integer", nullable: false),
                    ColorHex = table.Column<string>(type: "text", nullable: false),
                    PrimaryText = table.Column<string>(type: "text", nullable: false),
                    PrimaryTextDe = table.Column<string>(type: "text", nullable: false),
                    SecondaryText = table.Column<string>(type: "text", nullable: false),
                    SecondaryTextDe = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameDe = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<int>(type: "integer", nullable: false),
                    Prerequisites = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    TextDe = table.Column<string>(type: "text", nullable: false),
                    Expansion = table.Column<int>(type: "integer", nullable: false),
                    FactionId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technologies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgendaVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    Votes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendaVotes_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FactionId = table.Column<string>(type: "text", nullable: true),
                    ColorHex = table.Column<string>(type: "text", nullable: false),
                    SeatOrder = table.Column<int>(type: "integer", nullable: false),
                    HasPassed = table.Column<bool>(type: "boolean", nullable: false),
                    IsReady = table.Column<bool>(type: "boolean", nullable: false),
                    DeviceToken = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectiveId = table.Column<string>(type: "text", nullable: false),
                    RevealedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionObjectives_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategyCardStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyCardId = table.Column<int>(type: "integer", nullable: false),
                    TradeGoods = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyCardStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyCardStates_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStrategyCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyCardId = table.Column<int>(type: "integer", nullable: false),
                    IsExhausted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStrategyCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerStrategyCards_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTechnologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnologyId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTechnologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerTechnologies_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveScores_SessionObjectives_SessionObjectiveId",
                        column: x => x.SessionObjectiveId,
                        principalTable: "SessionObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaVotes_SessionId_PlayerId",
                table: "AgendaVotes",
                columns: new[] { "SessionId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveScores_SessionObjectiveId",
                table: "ObjectiveScores",
                column: "SessionObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_SessionId",
                table: "Players",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStrategyCards_PlayerId",
                table: "PlayerStrategyCards",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStrategyCards_SessionId_PlayerId",
                table: "PlayerStrategyCards",
                columns: new[] { "SessionId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTechnologies_PlayerId",
                table: "PlayerTechnologies",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTechnologies_SessionId_PlayerId",
                table: "PlayerTechnologies",
                columns: new[] { "SessionId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionObjectives_SessionId",
                table: "SessionObjectives",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_JoinCode",
                table: "Sessions",
                column: "JoinCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategyCardStates_SessionId_StrategyCardId",
                table: "StrategyCardStates",
                columns: new[] { "SessionId", "StrategyCardId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agendas");

            migrationBuilder.DropTable(
                name: "AgendaVotes");

            migrationBuilder.DropTable(
                name: "Factions");

            migrationBuilder.DropTable(
                name: "Objectives");

            migrationBuilder.DropTable(
                name: "ObjectiveScores");

            migrationBuilder.DropTable(
                name: "PlayerStrategyCards");

            migrationBuilder.DropTable(
                name: "PlayerTechnologies");

            migrationBuilder.DropTable(
                name: "StrategyCards");

            migrationBuilder.DropTable(
                name: "StrategyCardStates");

            migrationBuilder.DropTable(
                name: "Technologies");

            migrationBuilder.DropTable(
                name: "SessionObjectives");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Sessions");
        }
    }
}
