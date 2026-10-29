using System;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// F-163: the essay question of the Manaus 2026 municipal guard paper (Instituto Consulplan, booklet type 1), published
    /// with version 1 as paper number 61, its three supporting texts as one base text and the six criteria of the exam
    /// board's grid as its rubric. A data migration on the pattern of F-110 (<c>SeedManausGuardQuestions</c>): it runs once
    /// per database, never updates a row, and every value is a frozen literal, the derived columns included (BR2); a test ties
    /// them to the domain and the sanitizer. The text is in <c>SeedManausEssayQuestion.Data.cs</c>.
    /// <para>
    /// The block returns before any write when the catalog schema is missing, when the edition is missing or deleted, or when
    /// paper number 61 or the fixed essay id is already held, live or deleted (BR4). It reads the catalog read-only, the one
    /// exception to ADR-0005 recorded in F-110 change note v2. The topic is found by its seeded id, then by its normalized
    /// name in its subject; a missing or deleted topic skips the essay.
    /// </para>
    /// </summary>
    public partial class SeedManausEssayQuestion : Migration
    {
        private const int EditionGroup = 4;
        private const int SubjectGroup = 6;
        private const int TopicGroup = 7;
        private const int QuestionGroup = 10;
        private const int VersionGroup = 12;
        private const int BaseTextGroup = 13;
        private const int CriterionGroup = 15;
        private const int ManausEdition = 2;
        private const int ProductionSubject = 2;
        private const int EssayTopic = 2;
        private const int EssayNumber = 61;
        private const int SizeMin = 20;
        private const int SizeMax = 30;
        private const string TopicName = "REDACAO (NIVEL SUPERIOR)";
        private const string SubjectName = "PRODUCAO TEXTUAL";
        private const string SeededAt = "2026-10-28T12:00:00+00";
        private const string Language = "pt-BR";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var edition = Id(EditionGroup, ManausEdition);
            var essay = Id(QuestionGroup, EssayNumber);
            var baseText = Id(BaseTextGroup, 2);
            var sql = new StringBuilder();
            sql.AppendLine("DO $seed$");
            sql.AppendLine("DECLARE topic uuid;");
            sql.AppendLine("BEGIN");

            // BR4: every guard comes before the first statement that names a catalog table, so a database without the
            // catalog schema never resolves one.
            sql.AppendLine("IF to_regclass('catalog.exam_editions') IS NULL OR to_regclass('catalog.topics') IS NULL");
            sql.AppendLine("   OR to_regclass('catalog.subjects') IS NULL THEN RETURN; END IF;");
            sql.AppendLine($"IF NOT EXISTS (SELECT 1 FROM catalog.exam_editions e WHERE e.id = '{edition}' AND e.is_deleted = false) THEN RETURN; END IF;");
            sql.AppendLine($"IF EXISTS (SELECT 1 FROM question_bank.questions q WHERE q.id = '{essay}' OR (q.exam_edition_id = '{edition}' AND q.paper_number = {EssayNumber})) THEN RETURN; END IF;");

            var topicId = Id(TopicGroup, EssayTopic);
            var subjectId = Id(SubjectGroup, ProductionSubject);
            sql.AppendLine(
                $"""
                SELECT found.id INTO topic FROM (
                    SELECT t.id, t.is_deleted FROM catalog.topics t
                    WHERE t.tenant_id IS NULL
                      AND (t.id = '{topicId}'
                           OR (t.normalized_name = {Quote(TopicName)}
                               AND t.subject_id = (SELECT s.id FROM catalog.subjects s
                                                   WHERE s.tenant_id IS NULL AND (s.id = '{subjectId}' OR s.normalized_name = {Quote(SubjectName)})
                                                   ORDER BY (s.id = '{subjectId}') DESC, s.is_deleted LIMIT 1)))
                    ORDER BY (t.id = '{topicId}') DESC, t.is_deleted
                    LIMIT 1) found
                WHERE found.is_deleted = false;
                IF topic IS NULL THEN RETURN; END IF;
                """);

            sql.AppendLine(
                $"""
                INSERT INTO question_bank.base_texts (id, title, normalized_title, content_language, text_html, tenant_id, created_at, is_deleted)
                VALUES ('{baseText}', {Quote(BaseTextTitle)}, {Quote(BaseTextNormalizedTitle)}, '{Language}', {Quote(BaseTextHtml)}, NULL, '{SeededAt}', false);
                INSERT INTO question_bank.questions (id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement,
                    base_text_id, exam_edition_id, paper_number, notice_subject_id, answer_size_min, answer_size_max, tenant_id, created_at, is_deleted)
                VALUES ('{essay}', 'Essay', 'Published', topic, '{Language}', {Quote(StatementHtml)}, {Quote(StatementText)}, {Quote(NormalizedStatement)},
                    '{baseText}', '{edition}', {EssayNumber}, NULL, {SizeMin}, {SizeMax}, NULL, '{SeededAt}', false);
                """);

            // BR3: the rubric rows, in the order of the grid.
            for (var index = 0; index < Rubric.Length; index++)
            {
                var criterion = Rubric[index];
                sql.AppendLine(
                    $"""
                    INSERT INTO question_bank.question_rubric_criteria (id, question_id, position, name, description, max_points, tenant_id, created_at, is_deleted)
                    VALUES ('{Id(CriterionGroup, index + 1)}', '{essay}', {index + 1}, {Quote(criterion.Name)}, {Quote(criterion.Description)}, {criterion.MaxPoints.ToString(CultureInfo.InvariantCulture)}, NULL, '{SeededAt}', false);
                    """);
            }

            // BR2: version 1 as a publish writes it, with the base text and the rubric.
            sql.AppendLine(
                $"""
                INSERT INTO question_bank.question_versions (id, question_id, number, type, content_language, statement_html, base_text_html, explanation_html,
                    true_false_key, options, match_items, blanks, rubric, answer_size_min, answer_size_max, answer_key_changed, reason, annulled, tenant_id, created_at, is_deleted)
                VALUES ('{Id(VersionGroup, EssayNumber * 10 + 1)}', '{essay}', 1, 'Essay', '{Language}', {Quote(StatementHtml)}, {Quote(BaseTextHtml)}, NULL,
                    NULL, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, {Quote(RubricJson())}::jsonb, {SizeMin}, {SizeMax}, false, NULL, false, NULL, '{SeededAt}', false);
                """);

            sql.AppendLine("END $seed$;");
            migrationBuilder.Sql(sql.ToString());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // BR5: the essay leaves only when nobody changed it: its row was never updated and it has no version beyond the
            // one the seed wrote. What depends on it leaves with it; the base text stays while a question links it.
            var essay = Id(QuestionGroup, EssayNumber);
            migrationBuilder.Sql(
                $"""
                CREATE TEMPORARY TABLE seed_untouched ON COMMIT DROP AS
                SELECT q.id FROM question_bank.questions q
                WHERE q.id = '{essay}'
                  AND q.updated_at IS NULL
                  AND NOT EXISTS (SELECT 1 FROM question_bank.question_versions v WHERE v.question_id = q.id AND v.number > 1);
                DELETE FROM question_bank.question_rubric_criteria WHERE question_id IN (SELECT id FROM seed_untouched);
                DELETE FROM question_bank.question_versions WHERE question_id IN (SELECT id FROM seed_untouched);
                DELETE FROM question_bank.questions WHERE id IN (SELECT id FROM seed_untouched);
                DELETE FROM question_bank.base_texts b
                WHERE b.id = '{Id(BaseTextGroup, 2)}' AND b.updated_at IS NULL
                  AND NOT EXISTS (SELECT 1 FROM question_bank.questions q WHERE q.base_text_id = b.id);
                """);
        }

        // The rubric of a version as EF's ToJson stores it: PascalCase property names, in rubric order.
        private static string RubricJson()
        {
            var json = new StringBuilder("[");
            for (var index = 0; index < Rubric.Length; index++)
            {
                var criterion = Rubric[index];
                json.Append(index == 0 ? string.Empty : ", ")
                    .Append("{\"Description\": ").Append(JsonString(criterion.Description))
                    .Append(", \"MaxPoints\": ").Append(criterion.MaxPoints.ToString(CultureInfo.InvariantCulture))
                    .Append(", \"Name\": ").Append(JsonString(criterion.Name))
                    .Append(", \"Position\": ").Append((index + 1).ToString(CultureInfo.InvariantCulture)).Append('}');
            }

            return json.Append(']').ToString();
        }

        private static string JsonString(string value)
        {
            var builder = new StringBuilder("\"");
            foreach (var character in value)
            {
                builder.Append(character switch
                {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    _ => character.ToString()
                });
            }

            return builder.Append('"').ToString();
        }

        private static Guid Id(int group, int number) =>
            new($"0198f370-{group:D4}-7000-8000-{number:D12}");

        private static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

        private sealed record Criterion(string Name, string Description, int MaxPoints);
    }
}
