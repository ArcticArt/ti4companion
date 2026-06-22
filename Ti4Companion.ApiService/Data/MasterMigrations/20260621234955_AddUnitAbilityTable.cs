using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class AddUnitAbilityTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Technologies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Combat",
                table: "Technologies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CombatDice",
                table: "Technologies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Cost",
                table: "Technologies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Move",
                table: "Technologies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProducedCount",
                table: "Technologies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UnitAbilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TechnologyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ability = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    Dice = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitAbilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitAbilities_Technologies_TechnologyId",
                        column: x => x.TechnologyId,
                        principalTable: "Technologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnitAbilities_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitAbilities_TechnologyId",
                table: "UnitAbilities",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitAbilities_UnitId",
                table: "UnitAbilities",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitAbilities");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "Combat",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "CombatDice",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "Move",
                table: "Technologies");

            migrationBuilder.DropColumn(
                name: "ProducedCount",
                table: "Technologies");
        }
    }
}
