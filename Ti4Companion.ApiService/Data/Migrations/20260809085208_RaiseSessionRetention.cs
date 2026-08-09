using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ti4Companion.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RaiseSessionRetention : Migration
    {
        // Data-only: RetentionHours is stamped onto each session at creation time, so raising the
        // default from 168 h (7 days) to 2160 h (90 days) would otherwise only affect NEW sessions and
        // the existing games would still be wiped after a week. Only rows that still carry the old
        // default are touched, so a deliberately different window survives.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Sessions SET RetentionHours = 2160 WHERE RetentionHours = 168;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Sessions SET RetentionHours = 168 WHERE RetentionHours = 2160;");
        }
    }
}
