using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class DropUnitAbilitiesString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitAbilities",
                table: "Units");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnitAbilities",
                table: "Units",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
