using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dharmatlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClaimEvidenceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "interpretation",
                table: "claims",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "source_locator",
                table: "claims",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_claims_subject_publication",
                table: "claims",
                columns: new[] { "subject_entity_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_claims_subject_publication",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "interpretation",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "source_locator",
                table: "claims");
        }
    }
}
