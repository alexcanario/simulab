using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HideIssuingAuthorityAcronym : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_issuing_authorities_tenant_normalized_acronym",
                schema: "catalog",
                table: "issuing_authorities");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_acronym",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "The stored acronym without case or accents; no index reads it since F-44.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "The acronym without case or accents, which is what the unique index compares.");

            migrationBuilder.AlterColumn<string>(
                name: "acronym",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                comment: "Short name typed before it left the screens (F-44); kept, no longer set or searched.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Short name people search by, such as TRF1 or INSS.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "normalized_acronym",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                comment: "The acronym without case or accents, which is what the unique index compares.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "The stored acronym without case or accents; no index reads it since F-44.");

            migrationBuilder.AlterColumn<string>(
                name: "acronym",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                comment: "Short name people search by, such as TRF1 or INSS.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "Short name typed before it left the screens (F-44); kept, no longer set or searched.");

            migrationBuilder.CreateIndex(
                name: "ux_issuing_authorities_tenant_normalized_acronym",
                schema: "catalog",
                table: "issuing_authorities",
                columns: new[] { "tenant_id", "normalized_acronym" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
