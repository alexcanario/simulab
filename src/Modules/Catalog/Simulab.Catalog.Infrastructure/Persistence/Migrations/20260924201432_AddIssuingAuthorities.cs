using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuingAuthorities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_exams_issuing_authority",
                schema: "catalog",
                table: "exams");

            migrationBuilder.CreateTable(
                name: "issuing_authorities",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    acronym = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    normalized_acronym = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issuing_authorities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_issuing_authorities_tenant_normalized_acronym",
                schema: "catalog",
                table: "issuing_authorities",
                columns: new[] { "tenant_id", "normalized_acronym" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ux_issuing_authorities_tenant_normalized_name",
                schema: "catalog",
                table: "issuing_authorities",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            // F-34 v2: every exam written so far points at an organizer, and the new parent table is empty,
            // so the foreign key below would refuse to be created. The rows cannot be remapped - a board is
            // not the body that publishes the notice - and the catalog holds nothing but what was entered
            // while validating; F-37 brings the real data (owner, 2026-09-24).
            migrationBuilder.Sql("""
                DELETE FROM catalog.exams
                WHERE issuing_authority_id NOT IN (SELECT id FROM catalog.issuing_authorities);
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_exams_issuing_authority",
                schema: "catalog",
                table: "exams",
                column: "issuing_authority_id",
                principalSchema: "catalog",
                principalTable: "issuing_authorities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_exams_issuing_authority",
                schema: "catalog",
                table: "exams");

            migrationBuilder.DropTable(
                name: "issuing_authorities",
                schema: "catalog");

            migrationBuilder.AddForeignKey(
                name: "fk_exams_issuing_authority",
                schema: "catalog",
                table: "exams",
                column: "issuing_authority_id",
                principalSchema: "catalog",
                principalTable: "organizers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
