using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dharmatlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthenticatedContributionGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "submissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "submission_decisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "revisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_subject",
                table: "contributors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "roles",
                table: "contributors",
                type: "text",
                nullable: false,
                defaultValue: "[\"Contributor\"]");

            migrationBuilder.CreateIndex(
                name: "IX_contributors_external_subject",
                table: "contributors",
                column: "external_subject",
                unique: true,
                filter: "external_subject IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_contributors_external_subject",
                table: "contributors");

            migrationBuilder.DropColumn(
                name: "version",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "submission_decisions");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "revisions");

            migrationBuilder.DropColumn(
                name: "external_subject",
                table: "contributors");

            migrationBuilder.DropColumn(
                name: "roles",
                table: "contributors");
        }
    }
}
