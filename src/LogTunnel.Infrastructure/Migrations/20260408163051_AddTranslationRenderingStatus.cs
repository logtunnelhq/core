using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogTunnel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslationRenderingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_translations_status",
                table: "translations");

            migrationBuilder.AddCheckConstraint(
                name: "ck_translations_status",
                table: "translations",
                sql: "status IN ('pending','rendering','ready','failed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_translations_status",
                table: "translations");

            migrationBuilder.AddCheckConstraint(
                name: "ck_translations_status",
                table: "translations",
                sql: "status IN ('pending','ready','failed')");
        }
    }
}
