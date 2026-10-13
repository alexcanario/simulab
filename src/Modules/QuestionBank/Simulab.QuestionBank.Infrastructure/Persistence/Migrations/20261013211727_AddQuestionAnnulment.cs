using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionAnnulment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "question_bank",
                table: "questions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "Where the question is in its life: Draft, Published or Annulled.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "Where the question is in its life: Draft or Published.");

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                schema: "question_bank",
                table: "question_versions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Why the curator changed the answer key, annulled or reinstated the question, or a note on the change. Always set when answer_key_changed or annulled is true.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Why the curator changed the answer key, or a note on the change. Always set when answer_key_changed is true.");

            migrationBuilder.AddColumn<bool>(
                name: "annulled",
                schema: "question_bank",
                table: "question_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "True when the question was annulled when this version was recorded. A reinstatement is the first version after an annulled one with this false.");

            migrationBuilder.AddCheckConstraint(
                name: "ck_question_versions_annulled_key_changed",
                schema: "question_bank",
                table: "question_versions",
                sql: "annulled = false OR answer_key_changed = false");

            migrationBuilder.AddCheckConstraint(
                name: "ck_question_versions_annulled_reason",
                schema: "question_bank",
                table: "question_versions",
                sql: "annulled = false OR reason IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The previous binary does not know the status Annulled and cannot read such a row. They go back to
            // draft, which keeps them out of practice; the annulment version rows stay, without their flag.
            migrationBuilder.Sql("UPDATE question_bank.questions SET status = 'Draft' WHERE status = 'Annulled';");

            migrationBuilder.DropCheckConstraint(
                name: "ck_question_versions_annulled_key_changed",
                schema: "question_bank",
                table: "question_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_question_versions_annulled_reason",
                schema: "question_bank",
                table: "question_versions");

            migrationBuilder.DropColumn(
                name: "annulled",
                schema: "question_bank",
                table: "question_versions");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "question_bank",
                table: "questions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "Where the question is in its life: Draft or Published.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "Where the question is in its life: Draft, Published or Annulled.");

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                schema: "question_bank",
                table: "question_versions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Why the curator changed the answer key, or a note on the change. Always set when answer_key_changed is true.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Why the curator changed the answer key, annulled or reinstated the question, or a note on the change. Always set when answer_key_changed or annulled is true.");
        }
    }
}
