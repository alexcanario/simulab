using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrueFalseKey : Migration
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
                comment: "How the question is answered: SingleChoice or TrueFalse.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice.");

            migrationBuilder.AddColumn<bool>(
                name: "true_false_key",
                schema: "question_bank",
                table: "questions",
                type: "boolean",
                nullable: true,
                comment: "The key of a true/false question: true means right (certo), false wrong (errado), null not set yet. Always null for a single-choice question.");

            migrationBuilder.AddCheckConstraint(
                name: "ck_questions_true_false_key",
                schema: "question_bank",
                table: "questions",
                sql: "true_false_key IS NULL OR type = 'TrueFalse'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_questions_true_false_key",
                schema: "question_bank",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "true_false_key",
                schema: "question_bank",
                table: "questions");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "question_bank",
                table: "questions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How the question is answered: SingleChoice.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice or TrueFalse.");
        }
    }
}
