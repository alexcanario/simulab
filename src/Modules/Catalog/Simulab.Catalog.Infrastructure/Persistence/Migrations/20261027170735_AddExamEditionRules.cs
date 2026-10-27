using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamEditionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "points_per_question",
                schema: "catalog",
                table: "notice_subjects",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                comment: "What each question of the subject is worth, 0.01 to 100. Empty when the notice does not say.");

            migrationBuilder.AddColumn<int>(
                name: "duration_minutes",
                schema: "catalog",
                table: "exam_editions",
                type: "integer",
                nullable: true,
                comment: "How long the paper lasts, in whole minutes from 15 to 480. Empty means no time limit.");

            migrationBuilder.AddColumn<decimal>(
                name: "essay_cut_off_score",
                schema: "catalog",
                table: "exam_editions",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true,
                comment: "The essay points a candidate needs to pass, never above the essay maximum. Empty when not stated.");

            migrationBuilder.AddColumn<decimal>(
                name: "essay_max_score",
                schema: "catalog",
                table: "exam_editions",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true,
                comment: "The scale of the essay as the notice states it, 0.01 to 1000. Empty when there is no essay or it is not stated.");

            migrationBuilder.AddColumn<decimal>(
                name: "objective_cut_off_score",
                schema: "catalog",
                table: "exam_editions",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true,
                comment: "The points of the objective paper a candidate needs to pass, 0.01 to 1000. Empty when the notice says none.");

            migrationBuilder.AddCheckConstraint(
                name: "ck_notice_subjects_points_per_question",
                schema: "catalog",
                table: "notice_subjects",
                sql: "points_per_question IS NULL OR points_per_question BETWEEN 0.01 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_exam_editions_duration",
                schema: "catalog",
                table: "exam_editions",
                sql: "duration_minutes IS NULL OR duration_minutes BETWEEN 15 AND 480");

            migrationBuilder.AddCheckConstraint(
                name: "ck_exam_editions_essay_cut_off",
                schema: "catalog",
                table: "exam_editions",
                sql: "essay_cut_off_score IS NULL OR (essay_max_score IS NOT NULL AND essay_cut_off_score BETWEEN 0.01 AND essay_max_score)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_exam_editions_essay_max_score",
                schema: "catalog",
                table: "exam_editions",
                sql: "essay_max_score IS NULL OR essay_max_score BETWEEN 0.01 AND 1000");

            migrationBuilder.AddCheckConstraint(
                name: "ck_exam_editions_objective_cut_off",
                schema: "catalog",
                table: "exam_editions",
                sql: "objective_cut_off_score IS NULL OR objective_cut_off_score BETWEEN 0.01 AND 1000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_notice_subjects_points_per_question",
                schema: "catalog",
                table: "notice_subjects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_exam_editions_duration",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_exam_editions_essay_cut_off",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_exam_editions_essay_max_score",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_exam_editions_objective_cut_off",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropColumn(
                name: "points_per_question",
                schema: "catalog",
                table: "notice_subjects");

            migrationBuilder.DropColumn(
                name: "duration_minutes",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropColumn(
                name: "essay_cut_off_score",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropColumn(
                name: "essay_max_score",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropColumn(
                name: "objective_cut_off_score",
                schema: "catalog",
                table: "exam_editions");
        }
    }
}
