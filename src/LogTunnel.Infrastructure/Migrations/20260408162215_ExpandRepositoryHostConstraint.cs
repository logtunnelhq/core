using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogTunnel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandRepositoryHostConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_repositories_host",
                table: "repositories");

            migrationBuilder.AddCheckConstraint(
                name: "ck_repositories_host",
                table: "repositories",
                sql: "host IN ('github', 'gitlab', 'azure_devops')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_repositories_host",
                table: "repositories");

            migrationBuilder.AddCheckConstraint(
                name: "ck_repositories_host",
                table: "repositories",
                sql: "host IN ('github')");
        }
    }
}
