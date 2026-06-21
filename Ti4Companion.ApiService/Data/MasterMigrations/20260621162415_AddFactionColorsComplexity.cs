using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class AddFactionColorsComplexity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Complexity",
                table: "Factions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreferredColors",
                table: "Factions",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            // Bilingual labels for the two new enums (best-effort German).
            migrationBuilder.InsertData(
                table: "TypeValues",
                columns: new[] { "Type", "Value", "Name", "NameDe" },
                values: new object[,]
                {
                    { "PlayerColor", 0, "Purple", "Lila" },
                    { "PlayerColor", 1, "Pink", "Pink" },
                    { "PlayerColor", 2, "Red", "Rot" },
                    { "PlayerColor", 3, "Black", "Schwarz" },
                    { "PlayerColor", 4, "Blue", "Blau" },
                    { "PlayerColor", 5, "Green", "Grün" },
                    { "PlayerColor", 6, "Yellow", "Gelb" },
                    { "PlayerColor", 7, "Orange", "Orange" },
                    { "FactionComplexity", 0, "Low", "Niedrig" },
                    { "FactionComplexity", 1, "Moderate", "Mittel" },
                    { "FactionComplexity", 2, "High", "Hoch" },
                });

            // Per-faction preferred colours (ordered most→least, as PlayerColor ints) + complexity rating.
            migrationBuilder.Sql(@"
UPDATE ""Factions"" SET ""PreferredColors""='[0,1,2]',   ""Complexity""=0 WHERE ""Slug""='empyrean';
UPDATE ""Factions"" SET ""PreferredColors""='[2,3,4]',   ""Complexity""=0 WHERE ""Slug""='lizix';
UPDATE ""Factions"" SET ""PreferredColors""='[0,3]',     ""Complexity""=2 WHERE ""Slug""='obsidian';
UPDATE ""Factions"" SET ""PreferredColors""='[5,6,3]',   ""Complexity""=0 WHERE ""Slug""='nra';
UPDATE ""Factions"" SET ""PreferredColors""='[6,7]',     ""Complexity""=0 WHERE ""Slug""='hacan';
UPDATE ""Factions"" SET ""PreferredColors""='[5,6]',     ""Complexity""=0 WHERE ""Slug""='yssaril';
UPDATE ""Factions"" SET ""PreferredColors""='[0,3,6]',   ""Complexity""=0 WHERE ""Slug""='yin';
UPDATE ""Factions"" SET ""PreferredColors""='[7,6,3]',   ""Complexity""=2 WHERE ""Slug""='mentak';
UPDATE ""Factions"" SET ""PreferredColors""='[5,6,4]',   ""Complexity""=0 WHERE ""Slug""='lizards';
UPDATE ""Factions"" SET ""PreferredColors""='[6,0]',     ""Complexity""=2 WHERE ""Slug""='mahact';
UPDATE ""Factions"" SET ""PreferredColors""='[1]',       ""Complexity""=1 WHERE ""Slug""='titans';
UPDATE ""Factions"" SET ""PreferredColors""='[0,1,4,6]', ""Complexity""=1 WHERE ""Slug""='keleres';
UPDATE ""Factions"" SET ""PreferredColors""='[7,6,5]',   ""Complexity""=1 WHERE ""Slug""='saar';
UPDATE ""Factions"" SET ""PreferredColors""='[0,7,6]',   ""Complexity""=1 WHERE ""Slug""='winnu';
UPDATE ""Factions"" SET ""PreferredColors""='[0,4]',     ""Complexity""=0 WHERE ""Slug""='jolnar';
UPDATE ""Factions"" SET ""PreferredColors""='[3,2]',     ""Complexity""=0 WHERE ""Slug""='barony';
UPDATE ""Factions"" SET ""PreferredColors""='[2]',       ""Complexity""=2 WHERE ""Slug""='cabal';
UPDATE ""Factions"" SET ""PreferredColors""='[2,7]',     ""Complexity""=2 WHERE ""Slug""='muaat';
UPDATE ""Factions"" SET ""PreferredColors""='[5,7,6]',   ""Complexity""=1 WHERE ""Slug""='naalu';
UPDATE ""Factions"" SET ""PreferredColors""='[5,4]',     ""Complexity""=0 WHERE ""Slug""='xxcha';
UPDATE ""Factions"" SET ""PreferredColors""='[4,3,5]',   ""Complexity""=1 WHERE ""Slug""='deepwrought';
UPDATE ""Factions"" SET ""PreferredColors""='[4,6]',     ""Complexity""=0 WHERE ""Slug""='sol';
UPDATE ""Factions"" SET ""PreferredColors""='[5,3]',     ""Complexity""=2 WHERE ""Slug""='arborec';
UPDATE ""Factions"" SET ""PreferredColors""='[2]',       ""Complexity""=2 WHERE ""Slug""='crimson';
UPDATE ""Factions"" SET ""PreferredColors""='[4,0]',     ""Complexity""=0 WHERE ""Slug""='nomad';
UPDATE ""Factions"" SET ""PreferredColors""='[2,3]',     ""Complexity""=1 WHERE ""Slug""='sardakk';
UPDATE ""Factions"" SET ""PreferredColors""='[7,4]',     ""Complexity""=0 WHERE ""Slug""='bastion';
UPDATE ""Factions"" SET ""PreferredColors""='[4,3]',     ""Complexity""=1 WHERE ""Slug""='ghosts';
UPDATE ""Factions"" SET ""PreferredColors""='[2,3]',     ""Complexity""=2 WHERE ""Slug""='nekro';
UPDATE ""Factions"" SET ""PreferredColors""='[0,3]',     ""Complexity""=2 WHERE ""Slug""='firmament';
UPDATE ""Factions"" SET ""PreferredColors""='[7,6,5]',   ""Complexity""=0 WHERE ""Slug""='argent';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"TypeValues\" WHERE \"Type\" IN ('PlayerColor','FactionComplexity');");

            migrationBuilder.DropColumn(
                name: "Complexity",
                table: "Factions");

            migrationBuilder.DropColumn(
                name: "PreferredColors",
                table: "Factions");
        }
    }
}
