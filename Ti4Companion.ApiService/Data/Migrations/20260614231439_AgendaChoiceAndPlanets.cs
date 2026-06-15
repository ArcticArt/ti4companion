using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgendaChoiceAndPlanets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Choice",
                table: "AgendaVotes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Planets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameDe = table.Column<string>(type: "text", nullable: false),
                    Trait = table.Column<int>(type: "integer", nullable: false),
                    Resources = table.Column<int>(type: "integer", nullable: false),
                    Influence = table.Column<int>(type: "integer", nullable: false),
                    HomeFactionId = table.Column<string>(type: "text", nullable: true),
                    Legendary = table.Column<bool>(type: "boolean", nullable: false),
                    Expansion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planets", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Planets");

            migrationBuilder.DropColumn(
                name: "Choice",
                table: "AgendaVotes");
        }
    }
}
