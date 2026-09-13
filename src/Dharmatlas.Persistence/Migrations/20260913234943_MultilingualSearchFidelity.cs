using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dharmatlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultilingualSearchFidelity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_value",
                table: "entity_names",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Backfill for rows written before this column existed. lower(value)
            // approximates the shared NameNormalizer (casefold layer); every
            // subsequent insert or update stores the exact normalized form via
            // DharmatlasDbContext, so re-import fully corrects old rows.
            migrationBuilder.Sql("UPDATE entity_names SET normalized_value = lower(value) WHERE normalized_value = '';");

            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
                migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_entity_names_normalized_value_trgm ON entity_names USING gin (normalized_value gin_trgm_ops);");
                migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_entity_names_value_trgm ON entity_names USING gin (value gin_trgm_ops);");
            }

            migrationBuilder.CreateIndex(
                name: "ix_entity_names_normalized_value",
                table: "entity_names",
                column: "normalized_value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("DROP INDEX IF EXISTS ix_entity_names_normalized_value_trgm;");
                migrationBuilder.Sql("DROP INDEX IF EXISTS ix_entity_names_value_trgm;");
            }

            migrationBuilder.DropIndex(
                name: "ix_entity_names_normalized_value",
                table: "entity_names");

            migrationBuilder.DropColumn(
                name: "normalized_value",
                table: "entity_names");
        }
    }
}
