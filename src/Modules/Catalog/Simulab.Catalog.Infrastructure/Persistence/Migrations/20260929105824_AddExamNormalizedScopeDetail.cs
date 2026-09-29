using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamNormalizedScopeDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_scope_detail",
                schema: "catalog",
                table: "exams",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                comment: "The scope detail without case or accents, empty when there is none. The student search reads it.");

            // F-36 BR4: fill the rows that exist. CatalogText.Normalize strips accents and upper-cases, so the
            // translate step (both cases of every Latin letter a Brazilian or Portuguese place name can carry)
            // runs before upper(): under a "C" collation upper() leaves non-ASCII letters alone.
            migrationBuilder.Sql(
                """
                UPDATE catalog.exams
                SET normalized_scope_detail = upper(translate(
                    btrim(scope_detail),
                    'àáâãäåèéêëìíîïòóôõöùúûüýÿçñÀÁÂÃÄÅÈÉÊËÌÍÎÏÒÓÔÕÖÙÚÛÜÝÇÑ',
                    'aaaaaaeeeeiiiiooooouuuuyycnAAAAAAEEEEIIIIOOOOOUUUUYCN'))
                WHERE scope_detail IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "normalized_scope_detail",
                schema: "catalog",
                table: "exams");
        }
    }
}
