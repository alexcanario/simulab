using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// F-168 BR9: the exam rules of the Manaus 2026 edition (F-37) and the points per question of its nine notice
    /// subjects (F-78), from the notice the owner gave: Quadro I gives 2.00 points to Direito Constitucional and
    /// Legislação específica and 1.50 to the other seven (item 5.1.1), pass at 60 points (5.1.4), essay of 20 points
    /// (5.2.7), pass at 10 (5.2.9), 5 hours (6.1.2). A data migration with frozen literals: each value is written
    /// only where it is still empty, so a value an admin typed wins. The subjects are matched by their normalized
    /// label on the live rows of the edition, the nine named one by one, so a subject an admin added gets nothing.
    /// The essay cut-off is written only when the essay maximum (the one kept or the one seeded) is at least 10:
    /// otherwise the row would break <c>ck_exam_editions_essay_cut_off</c> and the migration would stop.
    /// </summary>
    public partial class SeedManausExamRules : Migration
    {
        private const string Manaus = "0198f370-0004-7000-8000-000000000002";

        private const string TwoPointLabels = "'DIREITO CONSTITUCIONAL', 'LEGISLACAO ESPECIFICA'";

        private const string OnePointFiftyLabels =
            "'LINGUA PORTUGUESA', 'INFORMATICA', 'GEOGRAFIA E HISTORIA DE MANAUS', 'ETICA E DIREITOS HUMANOS', "
            + "'DIREITO PENAL', 'DIREITO PROCESSUAL PENAL', 'LEGISLACAO DE TRANSITO'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                UPDATE catalog.exam_editions
                SET duration_minutes = COALESCE(duration_minutes, 300),
                    objective_cut_off_score = COALESCE(objective_cut_off_score, 60.00),
                    essay_max_score = COALESCE(essay_max_score, 20.00),
                    essay_cut_off_score = CASE
                        WHEN essay_cut_off_score IS NULL AND COALESCE(essay_max_score, 20.00) >= 10.00 THEN 10.00
                        ELSE essay_cut_off_score
                    END
                WHERE id = '{Manaus}' AND NOT is_deleted;
                """);

            migrationBuilder.Sql(
                $"""
                UPDATE catalog.notice_subjects
                SET points_per_question = 2.00
                WHERE exam_edition_id = '{Manaus}' AND NOT is_deleted AND points_per_question IS NULL
                  AND normalized_label IN ({TwoPointLabels});
                """);

            migrationBuilder.Sql(
                $"""
                UPDATE catalog.notice_subjects
                SET points_per_question = 1.50
                WHERE exam_edition_id = '{Manaus}' AND NOT is_deleted AND points_per_question IS NULL
                  AND normalized_label IN ({OnePointFiftyLabels});
                """);
        }

        /// <inheritdoc />
        /// <remarks>Clears only the values still equal to the seeded ones; an admin's other value stays.</remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                UPDATE catalog.exam_editions
                SET essay_cut_off_score = CASE WHEN essay_cut_off_score = 10.00 THEN NULL ELSE essay_cut_off_score END,
                    duration_minutes = CASE WHEN duration_minutes = 300 THEN NULL ELSE duration_minutes END,
                    objective_cut_off_score = CASE WHEN objective_cut_off_score = 60.00 THEN NULL ELSE objective_cut_off_score END
                WHERE id = '{Manaus}';

                UPDATE catalog.exam_editions
                SET essay_max_score = NULL
                WHERE id = '{Manaus}' AND essay_max_score = 20.00 AND essay_cut_off_score IS NULL;

                UPDATE catalog.notice_subjects
                SET points_per_question = NULL
                WHERE exam_edition_id = '{Manaus}'
                  AND ((points_per_question = 2.00 AND normalized_label IN ({TwoPointLabels}))
                    OR (points_per_question = 1.50 AND normalized_label IN ({OnePointFiftyLabels})));
                """);
        }
    }
}
