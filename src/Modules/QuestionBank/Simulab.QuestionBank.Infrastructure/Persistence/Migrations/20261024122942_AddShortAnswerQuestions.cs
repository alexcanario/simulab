using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShortAnswerQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "question_blanks",
                schema: "question_bank",
                comment: "One blank of a question and the answers it accepts: for a fill-in-the-blanks question a marker [[n]] of the statement, for a short-answer question its one blank numbered 1, with no marker.",
                oldComment: "One blank of a fill-in-the-blanks question: a marker [[n]] of the statement and the answers it accepts.");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "question_bank",
                table: "questions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching, FillInTheBlanks or ShortAnswer.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching or FillInTheBlanks.");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "question_bank",
                table: "question_versions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching, FillInTheBlanks or ShortAnswer.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching or FillInTheBlanks.");

            migrationBuilder.AlterColumn<Guid>(
                name: "question_id",
                schema: "question_bank",
                table: "question_blanks",
                type: "uuid",
                nullable: false,
                comment: "The fill-in-the-blanks or short-answer question the blank belongs to.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "The fill-in-the-blanks question the blank belongs to.");

            migrationBuilder.AlterColumn<int>(
                name: "position",
                schema: "question_bank",
                table: "question_blanks",
                type: "integer",
                nullable: false,
                comment: "Where its marker sits among the markers of the statement, from 0: the order the blanks are shown in (0 for a short-answer question).",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Where its marker sits among the markers of the statement, from 0: the order the blanks are shown in.");

            migrationBuilder.AlterColumn<int>(
                name: "number",
                schema: "question_bank",
                table: "question_blanks",
                type: "integer",
                nullable: false,
                comment: "The number of its marker [[n]] in the statement, 1 to 99 (always 1 for a short-answer question, which has no marker); unique for the question among the live rows.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "The number of its marker [[n]] in the statement, 1 to 99; unique for the question among the live rows.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "question_blanks",
                schema: "question_bank",
                comment: "One blank of a fill-in-the-blanks question: a marker [[n]] of the statement and the answers it accepts.",
                oldComment: "One blank of a question and the answers it accepts: for a fill-in-the-blanks question a marker [[n]] of the statement, for a short-answer question its one blank numbered 1, with no marker.");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "question_bank",
                table: "questions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching or FillInTheBlanks.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching, FillInTheBlanks or ShortAnswer.");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "question_bank",
                table: "question_versions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching or FillInTheBlanks.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How the question is answered: SingleChoice, TrueFalse, MultipleAnswer, Matching, FillInTheBlanks or ShortAnswer.");

            migrationBuilder.AlterColumn<Guid>(
                name: "question_id",
                schema: "question_bank",
                table: "question_blanks",
                type: "uuid",
                nullable: false,
                comment: "The fill-in-the-blanks question the blank belongs to.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "The fill-in-the-blanks or short-answer question the blank belongs to.");

            migrationBuilder.AlterColumn<int>(
                name: "position",
                schema: "question_bank",
                table: "question_blanks",
                type: "integer",
                nullable: false,
                comment: "Where its marker sits among the markers of the statement, from 0: the order the blanks are shown in.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Where its marker sits among the markers of the statement, from 0: the order the blanks are shown in (0 for a short-answer question).");

            migrationBuilder.AlterColumn<int>(
                name: "number",
                schema: "question_bank",
                table: "question_blanks",
                type: "integer",
                nullable: false,
                comment: "The number of its marker [[n]] in the statement, 1 to 99; unique for the question among the live rows.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "The number of its marker [[n]] in the statement, 1 to 99 (always 1 for a short-answer question, which has no marker); unique for the question among the live rows.");
        }
    }
}
