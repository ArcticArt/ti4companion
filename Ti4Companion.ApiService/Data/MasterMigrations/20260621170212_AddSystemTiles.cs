using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class AddSystemTiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemTiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TileNumber = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expansion = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    IsHomeSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    HomeFactionId = table.Column<string>(type: "TEXT", nullable: true),
                    IsAnomaly = table.Column<bool>(type: "INTEGER", nullable: false),
                    Anomalies = table.Column<int>(type: "INTEGER", nullable: false),
                    Wormholes = table.Column<int>(type: "INTEGER", nullable: false),
                    IsHyperlane = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFracture = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Planets = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemTiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemTiles_TileNumber",
                table: "SystemTiles",
                column: "TileNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemTiles");
        }
    }
}
