using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestionTypeComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "question_bank",
                table: "questions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How the question is answered: SingleChoice, TrueFalse or MultipleAnswer.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice or TrueFalse.");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "question_bank",
                table: "question_versions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How the question is answered: SingleChoice, TrueFalse or MultipleAnswer.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice or TrueFalse.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "question_bank",
                table: "questions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How the question is answered: SingleChoice or TrueFalse.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice, TrueFalse or MultipleAnswer.");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "question_bank",
                table: "question_versions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How the question is answered: SingleChoice or TrueFalse.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice, TrueFalse or MultipleAnswer.");
        }
    }
}
