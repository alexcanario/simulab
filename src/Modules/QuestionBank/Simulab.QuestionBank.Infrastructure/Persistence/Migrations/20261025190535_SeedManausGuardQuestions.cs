using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.QuestionBank.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// F-110: the 60 multiple-choice questions of the Manaus 2026 municipal guard paper (Instituto Consulplan, booklet
    /// type 1, final answer key), their base text and their three images, published with version 1; question 47 is then
    /// annulled. A data migration, so it runs once per database and a curator's later edit, unpublish, annulment or delete
    /// stays (BR1, BR9). Every value is a frozen literal, the derived columns included (BR2); a test ties them to the
    /// domain and the sanitizer. The questions themselves are in <c>SeedManausGuardQuestions.Data.cs</c>.
    /// <para>
    /// Change note v2, the one exception to ADR-0005: the edition, the topics and the notice subjects are read from the
    /// catalog schema, read-only and only here. Everything runs in one PL/pgSQL block that returns before the first catalog
    /// read when the schema is missing (the question bank migrated alone), when the edition is missing or deleted, or when
    /// it already holds a question, live or deleted (BR3). A topic is found by its seeded id, then by normalized name in
    /// its subject; a question whose topic is missing or deleted is skipped (BR4).
    /// </para>
    /// </summary>
    public partial class SeedManausGuardQuestions : Migration
    {
        private const int EditionGroup = 4;
        private const int SubjectGroup = 6;
        private const int TopicGroup = 7;
        private const int NoticeSubjectGroup = 8;
        private const int QuestionGroup = 10;
        private const int OptionGroup = 11;
        private const int VersionGroup = 12;
        private const int BaseTextGroup = 13;
        private const int ImageGroup = 14;
        private const int ManausEdition = 2;
        private const int AnnulledQuestion = 47;
        private const string AnnulmentReason = "Anulada pela banca no gabarito definitivo.";
        private const string SeededAt = "2026-10-25T12:00:00+00";
        private const string Language = "pt-BR";
        private const string TopicSeparator = " › ";

        // The canonical subjects the classification points at: the number of the F-51 seeded id and the normalized name.
        private static readonly Dictionary<string, Canonical> Subjects = new(StringComparer.Ordinal)
        {
            ["Leitura e Interpretação"] = new(1, "LEITURA E INTERPRETACAO"),
            ["Produção Textual"] = new(2, "PRODUCAO TEXTUAL"),
            ["Sintaxe"] = new(4, "SINTAXE"),
            ["Ortografia e Pontuação"] = new(5, "ORTOGRAFIA E PONTUACAO"),
            ["Informática"] = new(13, "INFORMATICA"),
            ["Direito Administrativo"] = new(14, "DIREITO ADMINISTRATIVO"),
            ["Direito Constitucional"] = new(15, "DIREITO CONSTITUCIONAL"),
            ["Direito Penal"] = new(16, "DIREITO PENAL"),
            ["Direitos Humanos"] = new(17, "DIREITOS HUMANOS"),
            ["Estatuto do Desarmamento"] = new(18, "ESTATUTO DO DESARMAMENTO"),
            ["Estatuto das Guardas Municipais"] = new(19, "ESTATUTO DAS GUARDAS MUNICIPAIS"),
            ["Legislação Municipal"] = new(20, "LEGISLACAO MUNICIPAL"),
            ["Legislação Federal"] = new(21, "LEGISLACAO FEDERAL"),
            ["Conhecimentos locais"] = new(23, "CONHECIMENTOS LOCAIS"),
        };

        // The canonical topics, keyed "Subject › Topic" as the approved table writes them.
        private static readonly Dictionary<string, Canonical> Topics = new(StringComparer.Ordinal)
        {
            ["Leitura e Interpretação › Interpretação de textos"] = new(1, "INTERPRETACAO DE TEXTOS"),
            ["Produção Textual › Comunicação escrita"] = new(3, "COMUNICACAO ESCRITA"),
            ["Sintaxe › Sintaxe, Concordância e Regência"] = new(5, "SINTAXE, CONCORDANCIA E REGENCIA"),
            ["Ortografia e Pontuação › Ortografia"] = new(6, "ORTOGRAFIA"),
            ["Informática › Sistemas operacionais Windows"] = new(24, "SISTEMAS OPERACIONAIS WINDOWS"),
            ["Informática › Pacotes Microsoft Office"] = new(25, "PACOTES MICROSOFT OFFICE"),
            ["Informática › Segurança na internet"] = new(27, "SEGURANCA NA INTERNET"),
            ["Informática › Conceitos de hardware"] = new(28, "CONCEITOS DE HARDWARE"),
            ["Direito Administrativo › Legislação administrativa"] = new(31, "LEGISLACAO ADMINISTRATIVA"),
            ["Direito Constitucional › Direitos fundamentais"] = new(35, "DIREITOS FUNDAMENTAIS"),
            ["Direito Constitucional › Constituição Federal (art. 144)"] = new(36, "CONSTITUICAO FEDERAL (ART. 144)"),
            ["Direito Constitucional › Princípios constitucionais"] = new(38, "PRINCIPIOS CONSTITUCIONAIS"),
            ["Direito Penal › Tipificação de crimes"] = new(39, "TIPIFICACAO DE CRIMES"),
            ["Direito Penal › Crimes específicos"] = new(43, "CRIMES ESPECIFICOS"),
            ["Direito Penal › Procedimentos processuais"] = new(44, "PROCEDIMENTOS PROCESSUAIS"),
            ["Direito Penal › Processo penal"] = new(45, "PROCESSO PENAL"),
            ["Direitos Humanos › Proteção de direitos fundamentais"] = new(46, "PROTECAO DE DIREITOS FUNDAMENTAIS"),
            ["Direitos Humanos › Legislação de direitos humanos"] = new(47, "LEGISLACAO DE DIREITOS HUMANOS"),
            ["Direitos Humanos › Direitos humanos e cidadania"] = new(48, "DIREITOS HUMANOS E CIDADANIA"),
            ["Estatuto do Desarmamento › Lei 10.826/2003 (Estatuto do Desarmamento)"] = new(50, "LEI 10.826/2003 (ESTATUTO DO DESARMAMENTO)"),
            ["Estatuto das Guardas Municipais › Lei 13.022/2014 (Estatuto das Guardas)"] = new(51, "LEI 13.022/2014 (ESTATUTO DAS GUARDAS)"),
            ["Legislação Municipal › Estatuto dos Funcionários Públicos Municipais"] = new(52, "ESTATUTO DOS FUNCIONARIOS PUBLICOS MUNICIPAIS"),
            ["Legislação Municipal › Legislação municipal específica"] = new(55, "LEGISLACAO MUNICIPAL ESPECIFICA"),
            ["Legislação Municipal › Organização municipal"] = new(56, "ORGANIZACAO MUNICIPAL"),
            ["Legislação Federal › Código de Trânsito"] = new(57, "CODIGO DE TRANSITO"),
            ["Legislação Federal › ECA (Lei 8.069/1990)"] = new(58, "ECA (LEI 8.069/1990)"),
            ["Legislação Federal › Lei Maria da Penha (Lei 11.340/2006)"] = new(59, "LEI MARIA DA PENHA (LEI 11.340/2006)"),
            ["Conhecimentos locais › Manaus (AM)"] = new(66, "MANAUS (AM)"),
        };

        // The Manaus 2026 notice subjects of F-78: the number of the seeded id and the normalized label.
        private static readonly Dictionary<string, Canonical> NoticeSubjects = new(StringComparer.Ordinal)
        {
            ["Língua Portuguesa"] = new(7, "LINGUA PORTUGUESA"),
            ["Informática"] = new(8, "INFORMATICA"),
            ["Geografia e História de Manaus"] = new(9, "GEOGRAFIA E HISTORIA DE MANAUS"),
            ["Ética e Direitos Humanos"] = new(10, "ETICA E DIREITOS HUMANOS"),
            ["Direito Constitucional"] = new(11, "DIREITO CONSTITUCIONAL"),
            ["Direito Penal"] = new(12, "DIREITO PENAL"),
            ["Direito Processual Penal"] = new(13, "DIREITO PROCESSUAL PENAL"),
            ["Legislação de trânsito"] = new(14, "LEGISLACAO DE TRANSITO"),
            ["Legislação específica"] = new(15, "LEGISLACAO ESPECIFICA"),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var edition = Id(EditionGroup, ManausEdition);
            var sql = new StringBuilder();
            sql.AppendLine("DO $seed$");
            sql.AppendLine("DECLARE topic uuid; notice uuid;");
            sql.AppendLine("BEGIN");

            // BR3 and change note v2: every guard comes before the first statement that names a catalog table, so a
            // database without the catalog schema never resolves one.
            sql.AppendLine("IF to_regclass('catalog.exam_editions') IS NULL OR to_regclass('catalog.topics') IS NULL");
            sql.AppendLine("   OR to_regclass('catalog.subjects') IS NULL OR to_regclass('catalog.notice_subjects') IS NULL THEN RETURN; END IF;");
            sql.AppendLine($"IF NOT EXISTS (SELECT 1 FROM catalog.exam_editions e WHERE e.id = '{edition}' AND e.is_deleted = false) THEN RETURN; END IF;");
            sql.AppendLine($"IF EXISTS (SELECT 1 FROM question_bank.questions q WHERE q.exam_edition_id = '{edition}') THEN RETURN; END IF;");

            // BR7: the base text of questions 01 to 08.
            sql.AppendLine(
                $"""
                INSERT INTO question_bank.base_texts (id, title, normalized_title, content_language, text_html, tenant_id, created_at, is_deleted)
                VALUES ('{Id(BaseTextGroup, 1)}', {Quote(BaseTextTitle)}, {Quote(BaseTextNormalizedTitle)}, '{Language}', {Quote(BaseTextHtml)}, NULL, '{SeededAt}', false);
                """);

            foreach (var question in Questions)
            {
                AppendQuestion(sql, question, edition);
            }

            // BR8: the image rows, owned by their question; a row whose question was skipped is not written.
            foreach (var image in Images)
            {
                var owner = Id(QuestionGroup, image.Question);
                sql.AppendLine(
                    $"""
                    INSERT INTO question_bank.question_images (id, blob_name, content_type, size_bytes, owner_kind, owner_id, tenant_id, created_at, is_deleted)
                    SELECT '{Id(ImageGroup, image.Number)}', 'question-images/{Id(ImageGroup, image.Number)}', 'image/png', {image.SizeBytes.ToString(CultureInfo.InvariantCulture)}, 'Question', '{owner}', NULL, '{SeededAt}', false
                    WHERE EXISTS (SELECT 1 FROM question_bank.questions q WHERE q.id = '{owner}');
                    """);
            }

            sql.AppendLine("END $seed$;");
            migrationBuilder.Sql(sql.ToString());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // BR9: a seeded question leaves only when nobody changed it: its row was never updated and it has no version
            // beyond the ones the seed wrote. What depends on it leaves with it; the base text stays while a question links it.
            var seeded = string.Join(", ", Questions.Select(question => $"'{Id(QuestionGroup, question.Number)}'"));
            migrationBuilder.Sql(
                $"""
                CREATE TEMPORARY TABLE seed_untouched ON COMMIT DROP AS
                SELECT q.id FROM question_bank.questions q
                WHERE q.id IN ({seeded})
                  AND q.updated_at IS NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM question_bank.question_versions v
                      WHERE v.question_id = q.id AND v.number > CASE WHEN q.paper_number = {AnnulledQuestion} THEN 2 ELSE 1 END);
                DELETE FROM question_bank.question_images WHERE owner_id IN (SELECT id FROM seed_untouched) AND id::text LIKE '0198f370-{ImageGroup:D4}-%';
                DELETE FROM question_bank.question_versions WHERE question_id IN (SELECT id FROM seed_untouched);
                DELETE FROM question_bank.question_options WHERE question_id IN (SELECT id FROM seed_untouched);
                DELETE FROM question_bank.questions WHERE id IN (SELECT id FROM seed_untouched);
                DELETE FROM question_bank.base_texts b
                WHERE b.id = '{Id(BaseTextGroup, 1)}' AND b.updated_at IS NULL
                  AND NOT EXISTS (SELECT 1 FROM question_bank.questions q WHERE q.base_text_id = b.id);
                """);
        }

        private static void AppendQuestion(StringBuilder sql, SeedQuestion question, Guid edition)
        {
            var id = Id(QuestionGroup, question.Number);
            var topic = Topics[question.Topic];
            var subject = Subjects[question.Topic[..question.Topic.IndexOf(TopicSeparator, StringComparison.Ordinal)]];
            var notice = NoticeSubjects[question.NoticeSubject];
            var topicId = Id(TopicGroup, topic.Number);
            var subjectId = Id(SubjectGroup, subject.Number);
            var noticeId = Id(NoticeSubjectGroup, notice.Number);
            var baseText = question.Number <= 8 ? $"'{Id(BaseTextGroup, 1)}'" : "NULL";
            var baseTextHtml = question.Number <= 8 ? Quote(BaseTextHtml) : "NULL";
            var annulled = question.Number == AnnulledQuestion;
            var correct = question.Key - 'A';
            var number = question.Number.ToString(CultureInfo.InvariantCulture);

            // BR4: the topic with the seeded id first, then the live one with the normalized name in its subject (itself
            // found by id, then by name); a deleted topic is a missing one. The notice subject is looked up the same way,
            // and a missing one leaves the question with none.
            sql.AppendLine(
                $"""
                topic := NULL;
                notice := NULL;
                SELECT found.id INTO topic FROM (
                    SELECT t.id, t.is_deleted FROM catalog.topics t
                    WHERE t.tenant_id IS NULL
                      AND (t.id = '{topicId}'
                           OR (t.normalized_name = {Quote(topic.NormalizedName)}
                               AND t.subject_id = (SELECT s.id FROM catalog.subjects s
                                                   WHERE s.tenant_id IS NULL AND (s.id = '{subjectId}' OR s.normalized_name = {Quote(subject.NormalizedName)})
                                                   ORDER BY (s.id = '{subjectId}') DESC, s.is_deleted LIMIT 1)))
                    ORDER BY (t.id = '{topicId}') DESC, t.is_deleted
                    LIMIT 1) found
                WHERE found.is_deleted = false;
                IF topic IS NOT NULL THEN
                SELECT n.id INTO notice FROM catalog.notice_subjects n
                WHERE n.tenant_id IS NULL AND n.is_deleted = false
                  AND (n.id = '{noticeId}' OR (n.exam_edition_id = '{edition}' AND n.normalized_label = {Quote(notice.NormalizedName)}))
                ORDER BY (n.id = '{noticeId}') DESC
                LIMIT 1;
                INSERT INTO question_bank.questions (id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement,
                    base_text_id, exam_edition_id, paper_number, notice_subject_id, tenant_id, created_at, is_deleted)
                VALUES ('{id}', 'SingleChoice', '{(annulled ? "Annulled" : "Published")}', topic, '{Language}', {Quote(question.StatementHtml)}, {Quote(question.StatementText)},
                    {Quote(question.NormalizedStatement)}, {baseText}, '{edition}', {number}, notice, NULL, '{SeededAt}', false);
                """);

            for (var position = 0; position < question.Options.Length; position++)
            {
                sql.AppendLine(
                    $"""
                    INSERT INTO question_bank.question_options (id, question_id, position, text_html, is_correct, tenant_id, created_at, is_deleted)
                    VALUES ('{Id(OptionGroup, question.Number * 10 + position + 1)}', '{id}', {position.ToString(CultureInfo.InvariantCulture)}, {Quote(question.Options[position])}, {(position == correct ? "true" : "false")}, NULL, '{SeededAt}', false);
                    """);
            }

            // BR5, BR6: version 1 as a publish writes it; question 47 then gets the annulment version, a copy of it.
            var options = Quote(OptionsJson(question, correct));
            AppendVersion(sql, question, id, 1, baseTextHtml, options, annulled: false);
            if (annulled)
            {
                AppendVersion(sql, question, id, 2, baseTextHtml, options, annulled: true);
            }

            sql.AppendLine("END IF;");
        }

        private static void AppendVersion(StringBuilder sql, SeedQuestion question, Guid id, int number, string baseTextHtml, string options, bool annulled) =>
            sql.AppendLine(
                $"""
                INSERT INTO question_bank.question_versions (id, question_id, number, type, content_language, statement_html, base_text_html, explanation_html,
                    true_false_key, options, match_items, blanks, rubric, answer_key_changed, reason, annulled, tenant_id, created_at, is_deleted)
                VALUES ('{Id(VersionGroup, question.Number * 10 + number)}', '{id}', {number.ToString(CultureInfo.InvariantCulture)}, 'SingleChoice', '{Language}', {Quote(question.StatementHtml)}, {baseTextHtml}, NULL,
                    NULL, {options}::jsonb, '[]'::jsonb, '[]'::jsonb, '[]'::jsonb, false, {(annulled ? Quote(AnnulmentReason) : "NULL")}, {(annulled ? "true" : "false")}, NULL, '{SeededAt}', false);
                """);

        // The options of a version as EF's ToJson stores them: PascalCase property names, in position order.
        private static string OptionsJson(SeedQuestion question, int correct) =>
            "[" + string.Join(", ", question.Options.Select((text, position) =>
                $"{{\"IsCorrect\": {(position == correct ? "true" : "false")}, \"Position\": {position.ToString(CultureInfo.InvariantCulture)}, \"TextHtml\": {JsonString(text)}}}")) + "]";

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

        private sealed record Canonical(int Number, string NormalizedName);

        private sealed record SeedImage(int Number, int Question, long SizeBytes);

        /// <param name="Number">The number in the paper.</param>
        /// <param name="NoticeSubject">The Manaus 2026 notice subject label.</param>
        /// <param name="Topic">"Subject › Topic" of the F-51 taxonomy.</param>
        /// <param name="Key">The correct option, A to D.</param>
        /// <param name="StatementHtml">The statement as the sanitizer returns it.</param>
        /// <param name="StatementText">Its plain text, as the sanitizer reads it.</param>
        /// <param name="NormalizedStatement">The comparable form of the plain text.</param>
        /// <param name="Options">The four options A to D, as the sanitizer returns them.</param>
        private sealed record SeedQuestion(
            int Number,
            string NoticeSubject,
            string Topic,
            char Key,
            string StatementHtml,
            string StatementText,
            string NormalizedStatement,
            string[] Options);
    }
}
