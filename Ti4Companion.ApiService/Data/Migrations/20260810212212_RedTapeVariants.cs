using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <summary>
    /// Red Tape becomes a choice of two published variants instead of one on/off flag, plus which strategy
    /// card carries the ability.
    ///
    /// The bool column is renamed rather than dropped so existing tables keep their setting — but the VALUES
    /// have to be remapped: the old flag meant the leaner variant, which is <c>RedTapeVariant.Lite</c> = 2,
    /// not 1 (<c>Bureaucracy</c>). A plain rename would silently switch every table to the other variant.
    /// </summary>
    public partial class RedTapeVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RedTapeLite",
                table: "SessionSummaries",
                newName: "RedTapeVariant");

            migrationBuilder.RenameColumn(
                name: "RedTapeLite",
                table: "Sessions",
                newName: "RedTapeVariant");

            migrationBuilder.AddColumn<int>(
                name: "RedTapeCardNumber",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // true (1) was the lean variant → Lite (2). Order matters: remap before anything reads it.
            migrationBuilder.Sql("UPDATE Sessions SET RedTapeVariant = 2 WHERE RedTapeVariant = 1;");
            migrationBuilder.Sql("UPDATE SessionSummaries SET RedTapeVariant = 2 WHERE RedTapeVariant = 1;");
            // A variant with no carrier card would leave nothing to remove the tape: default to Diplomacy,
            // the card both variants are written for.
            migrationBuilder.Sql("UPDATE Sessions SET RedTapeCardNumber = 2 WHERE RedTapeVariant <> 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Back to a bool: any variant becomes "on".
            migrationBuilder.Sql("UPDATE Sessions SET RedTapeVariant = 1 WHERE RedTapeVariant <> 0;");
            migrationBuilder.Sql("UPDATE SessionSummaries SET RedTapeVariant = 1 WHERE RedTapeVariant <> 0;");

            migrationBuilder.DropColumn(
                name: "RedTapeCardNumber",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "RedTapeVariant",
                table: "SessionSummaries",
                newName: "RedTapeLite");

            migrationBuilder.RenameColumn(
                name: "RedTapeVariant",
                table: "Sessions",
                newName: "RedTapeLite");
        }
    }
}
