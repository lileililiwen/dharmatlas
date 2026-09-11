using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dharmatlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EntityNameInvariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "value",
                table: "entity_names",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_entity_names_primary_language ON entity_names (entity_id, lower(language)) WHERE is_primary = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_entity_names_primary_language;");

            migrationBuilder.AlterColumn<string>(
                name: "value",
                table: "entity_names",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);
        }
    }
}
