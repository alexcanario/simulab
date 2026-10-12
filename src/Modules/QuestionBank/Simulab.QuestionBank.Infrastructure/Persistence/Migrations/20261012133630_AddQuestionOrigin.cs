using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "exam_edition_id",
                schema: "question_bank",
                table: "questions",
                type: "uuid",
                nullable: true,
                comment: "The past exam edition the question came from. Held by id; there is no foreign key to the catalog. Null means the question is authored.");

            migrationBuilder.AddColumn<Guid>(
                name: "notice_subject_id",
                schema: "question_bank",
                table: "questions",
                type: "uuid",
                nullable: true,
                comment: "The notice subject of that edition the question belongs to. Held by id; optional, and only with an edition.");

            migrationBuilder.AddColumn<int>(
                name: "paper_number",
                schema: "question_bank",
                table: "questions",
                type: "integer",
                nullable: true,
                comment: "The number of the question in that edition's paper, from 1 to 500. Set exactly when exam_edition_id is.");

            migrationBuilder.CreateIndex(
                name: "ix_questions_exam_edition",
                schema: "question_bank",
                table: "questions",
                column: "exam_edition_id");

            migrationBuilder.CreateIndex(
                name: "ix_questions_notice_subject",
                schema: "question_bank",
                table: "questions",
                column: "notice_subject_id");

            migrationBuilder.CreateIndex(
                name: "ux_questions_tenant_edition_paper_number",
                schema: "question_bank",
                table: "questions",
                columns: new[] { "tenant_id", "exam_edition_id", "paper_number" },
                unique: true,
                filter: "is_deleted = false AND exam_edition_id IS NOT NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_questions_origin",
                schema: "question_bank",
                table: "questions",
                sql: "(exam_edition_id IS NULL AND paper_number IS NULL AND notice_subject_id IS NULL) OR (exam_edition_id IS NOT NULL AND paper_number IS NOT NULL AND paper_number BETWEEN 1 AND 500)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_questions_exam_edition",
                schema: "question_bank",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "ix_questions_notice_subject",
                schema: "question_bank",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "ux_questions_tenant_edition_paper_number",
                schema: "question_bank",
                table: "questions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_questions_origin",
                schema: "question_bank",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "exam_edition_id",
                schema: "question_bank",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "notice_subject_id",
                schema: "question_bank",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "paper_number",
                schema: "question_bank",
                table: "questions");
        }
    }
}
