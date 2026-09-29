using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEnumColumnDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "scope",
                schema: "catalog",
                table: "exams",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How far the exam reaches: National, State or Municipal.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How far the exam reaches: Federal, State, Municipal, National or International.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "scope",
                schema: "catalog",
                table: "exams",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How far the exam reaches: Federal, State, Municipal, National or International.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How far the exam reaches: National, State or Municipal.");
        }
    }
}
