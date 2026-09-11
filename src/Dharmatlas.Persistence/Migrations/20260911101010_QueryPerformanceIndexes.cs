using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dharmatlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_relationships_from_id",
                table: "relationships",
                columns: new[] { "from_entity_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_relationships_to_id",
                table: "relationships",
                columns: new[] { "to_entity_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_entity_names_value_entity",
                table: "entity_names",
                columns: new[] { "value", "entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_relationships_from_id",
                table: "relationships");

            migrationBuilder.DropIndex(
                name: "ix_relationships_to_id",
                table: "relationships");

            migrationBuilder.DropIndex(
                name: "ix_entity_names_value_entity",
                table: "entity_names");
        }
    }
}
