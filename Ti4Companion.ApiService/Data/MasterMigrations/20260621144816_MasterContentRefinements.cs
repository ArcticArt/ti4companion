using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class MasterContentRefinements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Breakthroughs_Slug_Version",
                table: "Breakthroughs");

            migrationBuilder.DropColumn(
                name: "Expansion",
                table: "Breakthroughs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Breakthroughs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Breakthroughs");

            // Objectives: fold "secret" into Stage (=3). Preserve WHICH rows are secret BEFORE dropping the
            // bool, then add Phase (scoring phase) defaulting to Status (=3). This is deliberately NOT a
            // column rename — the old bool 0/1 values must not leak into Stage/Phase.
            migrationBuilder.Sql("UPDATE \"Objectives\" SET \"Stage\" = 3 WHERE \"IsSecret\" = 1;");
            migrationBuilder.DropColumn(name: "IsSecret", table: "Objectives");
            migrationBuilder.AddColumn<int>(
                name: "Phase",
                table: "Objectives",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            // Planets: drop the redundant NameDe (planet names aren't translated) and add EMPTY flavour
            // columns — again NOT a rename, so the old names don't become flavour text.
            migrationBuilder.DropColumn(name: "NameDe", table: "Planets");
            migrationBuilder.AddColumn<string>(
                name: "FlavorTextDe",
                table: "Planets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FlavorText",
                table: "Planets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsStation",
                table: "Planets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SystemTileId",
                table: "Planets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TechSkip1",
                table: "Planets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TechSkip2",
                table: "Planets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlavorText",
                table: "Leaders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FlavorTextDe",
                table: "Leaders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FlavorText",
                table: "Factions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FlavorTextDe",
                table: "Factions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConnectedColor1",
                table: "Breakthroughs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConnectedColor2",
                table: "Breakthroughs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TypeValues",
                columns: table => new
                {
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameDe = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeValues", x => new { x.Type, x.Value });
                });

            // Seed the bilingual enum-value labels (best-effort German — adjust in the DB as needed).
            migrationBuilder.InsertData(
                table: "TypeValues",
                columns: new[] { "Type", "Value", "Name", "NameDe" },
                values: new object[,]
                {
                    { "UnitType", 0, "None", "Keine" },
                    { "UnitType", 1, "Carrier", "Träger" },
                    { "UnitType", 2, "Cruiser", "Kreuzer" },
                    { "UnitType", 3, "Destroyer", "Zerstörer" },
                    { "UnitType", 4, "Fighter", "Jäger" },
                    { "UnitType", 5, "Infantry", "Infanterie" },
                    { "UnitType", 6, "PDS", "PDS" },
                    { "UnitType", 7, "Space Dock", "Raumdock" },
                    { "UnitType", 8, "Dreadnought", "Dreadnought" },
                    { "UnitType", 9, "War Sun", "Kriegssonne" },
                    { "UnitType", 10, "Flagship", "Flaggschiff" },
                    { "UnitType", 11, "Mech", "Mech" },
                    { "TechColor", 0, "Biotic", "Biotik" },
                    { "TechColor", 1, "Propulsion", "Antrieb" },
                    { "TechColor", 2, "Cybernetic", "Kybernetik" },
                    { "TechColor", 3, "Warfare", "Kriegsführung" },
                    { "TechColor", 4, "Unit", "Einheit" },
                    { "PlanetTrait", 0, "None", "Keine" },
                    { "PlanetTrait", 1, "Cultural", "Kulturell" },
                    { "PlanetTrait", 2, "Hazardous", "Gefährlich" },
                    { "PlanetTrait", 3, "Industrial", "Industriell" },
                    { "AgendaType", 0, "Law", "Gesetz" },
                    { "AgendaType", 1, "Directive", "Direktive" },
                    { "ObjectiveStage", 1, "Stage I", "Stufe I" },
                    { "ObjectiveStage", 2, "Stage II", "Stufe II" },
                    { "ObjectiveStage", 3, "Secret", "Geheim" },
                    { "LeaderType", 0, "Agent", "Agent" },
                    { "LeaderType", 1, "Commander", "Kommandant" },
                    { "LeaderType", 2, "Hero", "Held" },
                    { "GamePhase", 0, "Setup", "Aufbau" },
                    { "GamePhase", 1, "Strategy", "Strategie" },
                    { "GamePhase", 2, "Action", "Aktion" },
                    { "GamePhase", 3, "Status", "Status" },
                    { "GamePhase", 4, "Agenda", "Agenda" },
                    { "ContentSource", 0, "Base", "Grundspiel" },
                    { "ContentSource", 1, "Prophecy of Kings", "Prophecy of Kings" },
                    { "ContentSource", 2, "Codex I", "Codex I" },
                    { "ContentSource", 3, "Codex II", "Codex II" },
                    { "ContentSource", 4, "Codex III", "Codex III" },
                    { "ContentSource", 5, "Codex IV", "Codex IV" },
                    { "ContentSource", 6, "Thunder's Edge", "Thunder's Edge" },
                });

            migrationBuilder.CreateIndex(
                name: "IX_Breakthroughs_Slug",
                table: "Breakthroughs",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TypeValues");

            migrationBuilder.DropIndex(
                name: "IX_Breakthroughs_Slug",
                table: "Breakthroughs");

            migrationBuilder.DropColumn(
                name: "FlavorText",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "IsStation",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "SystemTileId",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "TechSkip1",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "TechSkip2",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "FlavorText",
                table: "Leaders");

            migrationBuilder.DropColumn(
                name: "FlavorTextDe",
                table: "Leaders");

            migrationBuilder.DropColumn(
                name: "FlavorText",
                table: "Factions");

            migrationBuilder.DropColumn(
                name: "FlavorTextDe",
                table: "Factions");

            migrationBuilder.DropColumn(
                name: "ConnectedColor1",
                table: "Breakthroughs");

            migrationBuilder.DropColumn(
                name: "ConnectedColor2",
                table: "Breakthroughs");

            migrationBuilder.DropColumn(name: "FlavorTextDe", table: "Planets");
            migrationBuilder.AddColumn<string>(
                name: "NameDe", table: "Planets", type: "TEXT", nullable: false, defaultValue: "");

            migrationBuilder.DropColumn(name: "Phase", table: "Objectives");
            migrationBuilder.AddColumn<bool>(
                name: "IsSecret", table: "Objectives", type: "INTEGER", nullable: false, defaultValue: false);
            migrationBuilder.Sql("UPDATE \"Objectives\" SET \"IsSecret\" = 1 WHERE \"Stage\" = 3;");

            migrationBuilder.AddColumn<int>(
                name: "Expansion",
                table: "Breakthroughs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Breakthroughs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Breakthroughs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Breakthroughs_Slug_Version",
                table: "Breakthroughs",
                columns: new[] { "Slug", "Version" },
                unique: true);
        }
    }
}
