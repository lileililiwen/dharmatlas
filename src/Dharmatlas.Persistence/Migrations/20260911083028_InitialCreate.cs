using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dharmatlas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    input_reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_text = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    model_version = table.Column<string>(type: "text", nullable: false),
                    prompt_ref = table.Column<string>(type: "text", nullable: true),
                    suggestion_json = table.Column<string>(type: "text", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_drafts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement = table.Column<string>(type: "text", nullable: false),
                    certainty = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    source_ids = table.Column<string>(type: "text", nullable: false),
                    subject_entity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claims", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contributors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contributors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    when_kind = table.Column<string>(type: "text", nullable: true),
                    when_display = table.Column<string>(type: "text", nullable: true),
                    when_lower = table.Column<int>(type: "integer", nullable: true),
                    when_upper = table.Column<int>(type: "integer", nullable: true),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "text", nullable: true),
                    region = table.Column<string>(type: "text", nullable: true),
                    certainty = table.Column<string>(type: "text", nullable: true),
                    InstitutionalForm = table.Column<string>(type: "text", nullable: true),
                    activity_kind = table.Column<string>(type: "text", nullable: true),
                    activity_display = table.Column<string>(type: "text", nullable: true),
                    activity_lower = table.Column<int>(type: "integer", nullable: true),
                    activity_upper = table.Column<int>(type: "integer", nullable: true),
                    source_ids = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    modern_name = table.Column<string>(type: "text", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: true),
                    original_language = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entity_names",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    script = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    romanization = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_names", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "relationships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    certainty = table.Column<string>(type: "text", nullable: false),
                    source_ids = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relationships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prior_value_json = table.Column<string>(type: "text", nullable: false),
                    contributor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    changed_fields_json = table.Column<string>(type: "text", nullable: true),
                    source_ids = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revisions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    author = table.Column<string>(type: "text", nullable: true),
                    date = table.Column<string>(type: "text", nullable: true),
                    publisher_or_collection = table.Column<string>(type: "text", nullable: true),
                    identifier = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contributor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    source_ids = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_draft_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    edited_suggestion_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_draft_decisions", x => new { x.ai_draft_id, x.id });
                    table.ForeignKey(
                        name: "FK_ai_draft_decisions_ai_drafts_ai_draft_id",
                        column: x => x.ai_draft_id,
                        principalTable: "ai_drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "submission_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submission_decisions", x => new { x.submission_id, x.id });
                    table.ForeignKey(
                        name: "FK_submission_decisions_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_drafts_input",
                table: "ai_drafts",
                column: "input_reference_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_drafts_status",
                table: "ai_drafts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_claims_certainty_status",
                table: "claims",
                columns: new[] { "certainty", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_claims_subject",
                table: "claims",
                column: "subject_entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_entities_type",
                table: "entities",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "ix_events_category_region",
                table: "entities",
                columns: new[] { "category", "region" });

            migrationBuilder.CreateIndex(
                name: "ix_events_when_range",
                table: "entities",
                columns: new[] { "when_lower", "when_upper" });

            migrationBuilder.CreateIndex(
                name: "ix_institutions_activity_range",
                table: "entities",
                columns: new[] { "activity_lower", "activity_upper" });

            migrationBuilder.CreateIndex(
                name: "ix_places_activity_range",
                table: "entities",
                columns: new[] { "activity_lower", "activity_upper" });

            migrationBuilder.CreateIndex(
                name: "ix_places_coords",
                table: "entities",
                columns: new[] { "latitude", "longitude" });

            migrationBuilder.CreateIndex(
                name: "ix_places_kind",
                table: "entities",
                column: "kind");

            migrationBuilder.CreateIndex(
                name: "ix_entity_names_lang_value",
                table: "entity_names",
                columns: new[] { "language", "value" });

            migrationBuilder.CreateIndex(
                name: "ix_entity_names_romanization",
                table: "entity_names",
                column: "romanization");

            migrationBuilder.CreateIndex(
                name: "ix_entity_names_script",
                table: "entity_names",
                column: "script");

            migrationBuilder.CreateIndex(
                name: "ix_entity_names_value",
                table: "entity_names",
                column: "value");

            migrationBuilder.CreateIndex(
                name: "ix_relationships_endpoints",
                table: "relationships",
                columns: new[] { "from_entity_id", "to_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_relationships_type",
                table: "relationships",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_revisions_target",
                table: "revisions",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "ix_sources_identifier",
                table: "sources",
                column: "identifier");

            migrationBuilder.CreateIndex(
                name: "ix_submissions_status",
                table: "submissions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_submissions_target",
                table: "submissions",
                column: "target_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_draft_decisions");

            migrationBuilder.DropTable(
                name: "claims");

            migrationBuilder.DropTable(
                name: "contributors");

            migrationBuilder.DropTable(
                name: "entities");

            migrationBuilder.DropTable(
                name: "entity_names");

            migrationBuilder.DropTable(
                name: "relationships");

            migrationBuilder.DropTable(
                name: "revisions");

            migrationBuilder.DropTable(
                name: "sources");

            migrationBuilder.DropTable(
                name: "submission_decisions");

            migrationBuilder.DropTable(
                name: "ai_drafts");

            migrationBuilder.DropTable(
                name: "submissions");
        }
    }
}
