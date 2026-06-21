using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class MasterInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agendas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Elect = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TextDe = table.Column<string>(type: "TEXT", nullable: false),
                    RemovedInPok = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Breakthroughs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    FactionId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TextDe = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Breakthroughs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FactionAbilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    FactionId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TextDe = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionAbilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Factions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", nullable: false),
                    InitiativeOverride = table.Column<int>(type: "INTEGER", nullable: true),
                    IconPath = table.Column<string>(type: "TEXT", nullable: true),
                    StartingTechnologies = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FactionStartingUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FactionId = table.Column<string>(type: "TEXT", nullable: false),
                    UnitId = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionStartingUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leaders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    FactionId = table.Column<string>(type: "TEXT", nullable: false),
                    LeaderType = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TextDe = table.Column<string>(type: "TEXT", nullable: false),
                    UnlockCondition = table.Column<string>(type: "TEXT", nullable: false),
                    UnlockConditionDe = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Objectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    Requirement = table.Column<string>(type: "TEXT", nullable: false),
                    RequirementDe = table.Column<string>(type: "TEXT", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objectives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Planets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    Trait = table.Column<int>(type: "INTEGER", nullable: false),
                    Trait2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Resources = table.Column<int>(type: "INTEGER", nullable: false),
                    Influence = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeFactionId = table.Column<string>(type: "TEXT", nullable: true),
                    Legendary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategyCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    Initiative = table.Column<int>(type: "INTEGER", nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", nullable: false),
                    PrimaryText = table.Column<string>(type: "TEXT", nullable: false),
                    PrimaryTextDe = table.Column<string>(type: "TEXT", nullable: false),
                    SecondaryText = table.Column<string>(type: "TEXT", nullable: false),
                    SecondaryTextDe = table.Column<string>(type: "TEXT", nullable: false),
                    RevisionLabel = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    Prerequisites = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TextDe = table.Column<string>(type: "TEXT", nullable: false),
                    FactionId = table.Column<string>(type: "TEXT", nullable: true),
                    UnitType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technologies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false),
                    UnitType = table.Column<int>(type: "INTEGER", nullable: false),
                    FactionId = table.Column<string>(type: "TEXT", nullable: true),
                    Cost = table.Column<int>(type: "INTEGER", nullable: true),
                    ProducedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Combat = table.Column<int>(type: "INTEGER", nullable: true),
                    CombatDice = table.Column<int>(type: "INTEGER", nullable: false),
                    Move = table.Column<int>(type: "INTEGER", nullable: true),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TextDe = table.Column<string>(type: "TEXT", nullable: false),
                    UnitAbilities = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agendas_Slug_Version",
                table: "Agendas",
                columns: new[] { "Slug", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Breakthroughs_FactionId",
                table: "Breakthroughs",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Breakthroughs_Slug_Version",
                table: "Breakthroughs",
                columns: new[] { "Slug", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactionAbilities_FactionId",
                table: "FactionAbilities",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionAbilities_Slug_Version",
                table: "FactionAbilities",
                columns: new[] { "Slug", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factions_Slug_Version",
                table: "Factions",
                columns: new[] { "Slug", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactionStartingUnits_FactionId",
                table: "FactionStartingUnits",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaders_FactionId",
                table: "Leaders",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaders_Slug_Version",
                table: "Leaders",
                columns: new[] { "Slug", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_Slug_Version",
                table: "Objectives",
                columns: new[] { "Slug", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Planets_Slug_Version",
                table: "Planets",
                columns: new[] { "Slug", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategyCards_Number_Version",
                table: "StrategyCards",
                columns: new[] { "Number", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Slug_Version",
                table: "Technologies",
                columns: new[] { "Slug", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_Slug_Version",
                table: "Units",
                columns: new[] { "Slug", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agendas");

            migrationBuilder.DropTable(
                name: "Breakthroughs");

            migrationBuilder.DropTable(
                name: "FactionAbilities");

            migrationBuilder.DropTable(
                name: "Factions");

            migrationBuilder.DropTable(
                name: "FactionStartingUnits");

            migrationBuilder.DropTable(
                name: "Leaders");

            migrationBuilder.DropTable(
                name: "Objectives");

            migrationBuilder.DropTable(
                name: "Planets");

            migrationBuilder.DropTable(
                name: "StrategyCards");

            migrationBuilder.DropTable(
                name: "Technologies");

            migrationBuilder.DropTable(
                name: "Units");
        }
    }
}
