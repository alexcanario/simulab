using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// F-78: the notice subjects of the four municipal guard editions F-37 brought in, with their mapping to the
    /// canonical subjects and topics of F-51 — read from the official notices and checked by the owner on
    /// 2026-10-19. A data migration, so it runs once per database and an admin's edit, move, remap or delete
    /// stays final (BR1, BR6). The values are frozen literals, normalized columns included, never computed from
    /// code that evolves (BR2); a test ties them to the domain rules.
    /// An edition that is deleted or already holds any notice subject, live or deleted, is skipped whole (BR3).
    /// A mapping target is found by its seeded id, then by normalized name, and a missing or deleted one is
    /// skipped (BR4): the inserts are SQL, because <c>InsertData</c> cannot skip or look up.
    /// </summary>
    public partial class SeedGuardNoticeSubjects : Migration
    {
        private const int EditionGroup = 4;
        private const int SubjectGroup = 6;
        private const int TopicGroup = 7;
        private const int NoticeSubjectGroup = 8;
        private const int MappingGroup = 9;
        private const string SeededAt = "2026-10-19T12:00:00+00";
        private const string TopicSeparator = " › ";

        private const string ModuleOne = "Módulo I - Conhecimentos Básicos";
        private const string ModuleOneNormalized = "MODULO I - CONHECIMENTOS BASICOS";
        private const string ModuleTwo = "Módulo II - Conhecimentos Específicos";
        private const string ModuleTwoNormalized = "MODULO II - CONHECIMENTOS ESPECIFICOS";

        // The canonical subjects the lists point at, with the number of the seeded id (F-51) and the normalized name.
        private static readonly Dictionary<string, CanonicalSubject> Subjects = new(StringComparer.Ordinal)
        {
            ["Leitura e Interpretação"] = new(1, "LEITURA E INTERPRETACAO"),
            ["Produção Textual"] = new(2, "PRODUCAO TEXTUAL"),
            ["Morfologia"] = new(3, "MORFOLOGIA"),
            ["Sintaxe"] = new(4, "SINTAXE"),
            ["Ortografia e Pontuação"] = new(5, "ORTOGRAFIA E PONTUACAO"),
            ["Semântica e Léxico"] = new(6, "SEMANTICA E LEXICO"),
            ["Aritmética e Proporcionalidade"] = new(7, "ARITMETICA E PROPORCIONALIDADE"),
            ["Álgebra e Conjuntos"] = new(8, "ALGEBRA E CONJUNTOS"),
            ["Geometria"] = new(9, "GEOMETRIA"),
            ["Estatística e Tratamento de Informações"] = new(10, "ESTATISTICA E TRATAMENTO DE INFORMACOES"),
            ["Lógica Proposicional"] = new(11, "LOGICA PROPOSICIONAL"),
            ["Raciocínio Analítico"] = new(12, "RACIOCINIO ANALITICO"),
            ["Informática"] = new(13, "INFORMATICA"),
            ["Direito Administrativo"] = new(14, "DIREITO ADMINISTRATIVO"),
            ["Direito Constitucional"] = new(15, "DIREITO CONSTITUCIONAL"),
            ["Direito Penal"] = new(16, "DIREITO PENAL"),
            ["Direitos Humanos"] = new(17, "DIREITOS HUMANOS"),
            ["Estatuto do Desarmamento"] = new(18, "ESTATUTO DO DESARMAMENTO"),
            ["Estatuto das Guardas Municipais"] = new(19, "ESTATUTO DAS GUARDAS MUNICIPAIS"),
            ["Legislação Municipal"] = new(20, "LEGISLACAO MUNICIPAL"),
            ["Legislação Federal"] = new(21, "LEGISLACAO FEDERAL"),
            ["Procedimentos Policiais"] = new(22, "PROCEDIMENTOS POLICIAIS"),
            ["Conhecimentos locais"] = new(23, "CONHECIMENTOS LOCAIS"),
        };

        // The canonical topics the lists point at, keyed "Subject › Topic".
        private static readonly Dictionary<string, CanonicalTopic> Topics = new(StringComparer.Ordinal)
        {
            ["Produção Textual › Comunicação escrita"] = new(3, "COMUNICACAO ESCRITA"),
            ["Direito Administrativo › Legislação administrativa"] = new(31, "LEGISLACAO ADMINISTRATIVA"),
            ["Direito Constitucional › Direitos fundamentais"] = new(35, "DIREITOS FUNDAMENTAIS"),
            ["Direito Penal › Crimes específicos"] = new(43, "CRIMES ESPECIFICOS"),
            ["Direito Penal › Procedimentos processuais"] = new(44, "PROCEDIMENTOS PROCESSUAIS"),
            ["Direito Penal › Processo penal"] = new(45, "PROCESSO PENAL"),
            ["Legislação Federal › Código de Trânsito"] = new(57, "CODIGO DE TRANSITO"),
            ["Legislação Federal › ECA (Lei 8.069/1990)"] = new(58, "ECA (LEI 8.069/1990)"),
            ["Legislação Federal › Lei Maria da Penha (Lei 11.340/2006)"] = new(59, "LEI MARIA DA PENHA (LEI 11.340/2006)"),
            ["Procedimentos Policiais › Progressividade da força"] = new(61, "PROGRESSIVIDADE DA FORCA"),
            ["Conhecimentos locais › Curitiba (PR)"] = new(65, "CURITIBA (PR)"),
            ["Conhecimentos locais › Manaus (AM)"] = new(66, "MANAUS (AM)"),
            ["Conhecimentos locais › Salvador (BA)"] = new(67, "SALVADOR (BA)"),
            ["Conhecimentos locais › Maceió (AL)"] = new(70, "MACEIO (AL)"),
        };

        // The order is frozen: the position (1 to 28) is the tail of the notice subject's fixed id, and a mapping
        // entry's id tail is that position times 100 plus its own position in the list. The edition is the tail
        // of the F-37 edition id; rows of one edition are consecutive and keep the notice's order.
        private static readonly SeedNoticeSubject[] NoticeSubjects =
        [
            // 1 Curitiba 2025 — 40 questions
            new(1, null, "", "Língua Portuguesa", "LINGUA PORTUGUESA", 5,
                ["Leitura e Interpretação", "Morfologia", "Sintaxe", "Ortografia e Pontuação", "Semântica e Léxico"]),
            new(1, null, "", "Raciocínio Lógico e Matemático", "RACIOCINIO LOGICO E MATEMATICO", 5,
                ["Aritmética e Proporcionalidade", "Álgebra e Conjuntos", "Geometria", "Estatística e Tratamento de Informações", "Lógica Proposicional", "Raciocínio Analítico"]),
            new(1, null, "", "Noções de Informática", "NOCOES DE INFORMATICA", 5, ["Informática"]),
            new(1, null, "", "História e Geografia de Curitiba", "HISTORIA E GEOGRAFIA DE CURITIBA", 5, ["Conhecimentos locais › Curitiba (PR)"]),
            new(1, null, "", "Noções de Direito (Constitucional, Administrativo, Penal e Processual Penal)",
                "NOCOES DE DIREITO (CONSTITUCIONAL, ADMINISTRATIVO, PENAL E PROCESSUAL PENAL)", 10,
                ["Direito Administrativo", "Direito Constitucional", "Direito Penal"]),
            new(1, null, "", "Legislação", "LEGISLACAO", 10,
                ["Legislação Municipal", "Estatuto das Guardas Municipais", "Estatuto do Desarmamento", "Legislação Federal",
                 "Direito Administrativo › Legislação administrativa", "Direito Penal › Crimes específicos",
                 "Direito Constitucional › Direitos fundamentais", "Procedimentos Policiais › Progressividade da força"]),

            // 2 Manaus 2026 — 60 questions
            new(2, null, "", "Língua Portuguesa", "LINGUA PORTUGUESA", 10,
                ["Leitura e Interpretação", "Produção Textual › Comunicação escrita", "Morfologia", "Sintaxe", "Ortografia e Pontuação", "Semântica e Léxico"]),
            new(2, null, "", "Informática", "INFORMATICA", 5, ["Informática"]),
            new(2, null, "", "Geografia e História de Manaus", "GEOGRAFIA E HISTORIA DE MANAUS", 5, ["Conhecimentos locais › Manaus (AM)"]),
            new(2, null, "", "Ética e Direitos Humanos", "ETICA E DIREITOS HUMANOS", 5, ["Direitos Humanos"]),
            new(2, null, "", "Direito Constitucional", "DIREITO CONSTITUCIONAL", 5, ["Direito Constitucional"]),
            new(2, null, "", "Direito Penal", "DIREITO PENAL", 5, ["Direito Penal"]),
            new(2, null, "", "Direito Processual Penal", "DIREITO PROCESSUAL PENAL", 5,
                ["Direito Penal › Procedimentos processuais", "Direito Penal › Processo penal"]),
            new(2, null, "", "Legislação de trânsito", "LEGISLACAO DE TRANSITO", 5, ["Legislação Federal › Código de Trânsito"]),
            new(2, null, "", "Legislação específica", "LEGISLACAO ESPECIFICA", 15,
                ["Estatuto das Guardas Municipais", "Estatuto do Desarmamento", "Legislação Municipal", "Conhecimentos locais › Manaus (AM)",
                 "Legislação Federal › ECA (Lei 8.069/1990)", "Legislação Federal › Lei Maria da Penha (Lei 11.340/2006)",
                 "Direito Administrativo › Legislação administrativa", "Direito Penal › Crimes específicos"]),

            // 3 Salvador 2026 — 70 questions, two blocks
            new(3, ModuleOne, ModuleOneNormalized, "Língua Portuguesa", "LINGUA PORTUGUESA", 10,
                ["Leitura e Interpretação", "Produção Textual", "Morfologia", "Sintaxe", "Ortografia e Pontuação", "Semântica e Léxico"]),
            new(3, ModuleOne, ModuleOneNormalized, "Raciocínio Lógico-Matemático", "RACIOCINIO LOGICO-MATEMATICO", 10,
                ["Aritmética e Proporcionalidade", "Álgebra e Conjuntos", "Geometria", "Estatística e Tratamento de Informações", "Lógica Proposicional", "Raciocínio Analítico"]),
            new(3, ModuleOne, ModuleOneNormalized, "Informática", "INFORMATICA", 8, ["Informática"]),
            new(3, ModuleTwo, ModuleTwoNormalized, "Noções de Direito Constitucional e Direito Civil",
                "NOCOES DE DIREITO CONSTITUCIONAL E DIREITO CIVIL", 10, ["Direito Constitucional"]),
            new(3, ModuleTwo, ModuleTwoNormalized, "Noções de Direito Penal e Direito Processual Penal",
                "NOCOES DE DIREITO PENAL E DIREITO PROCESSUAL PENAL", 7, ["Direito Penal"]),
            new(3, ModuleTwo, ModuleTwoNormalized, "Noções de Administração e Políticas Públicas",
                "NOCOES DE ADMINISTRACAO E POLITICAS PUBLICAS", 5, []),
            new(3, ModuleTwo, ModuleTwoNormalized, "Conhecimentos na Área de Atuação", "CONHECIMENTOS NA AREA DE ATUACAO", 10,
                ["Procedimentos Policiais"]),
            new(3, ModuleTwo, ModuleTwoNormalized, "Legislação", "LEGISLACAO", 10,
                ["Legislação Federal", "Estatuto do Desarmamento", "Estatuto das Guardas Municipais", "Legislação Municipal",
                 "Conhecimentos locais › Salvador (BA)", "Direito Penal › Crimes específicos"]),

            // 4 Maceió 2026 — 60 questions
            new(4, null, "", "Língua Portuguesa", "LINGUA PORTUGUESA", 10,
                ["Leitura e Interpretação", "Morfologia", "Sintaxe", "Ortografia e Pontuação", "Semântica e Léxico"]),
            new(4, null, "", "Raciocínio Lógico", "RACIOCINIO LOGICO", 5,
                ["Aritmética e Proporcionalidade", "Álgebra e Conjuntos", "Geometria", "Estatística e Tratamento de Informações", "Lógica Proposicional", "Raciocínio Analítico"]),
            new(4, null, "", "Noções de Informática", "NOCOES DE INFORMATICA", 5, ["Informática"]),
            new(4, null, "", "Legislação Municipal Específica", "LEGISLACAO MUNICIPAL ESPECIFICA", 10,
                ["Legislação Municipal", "Conhecimentos locais › Maceió (AL)"]),
            new(4, null, "", "Conhecimentos Específicos", "CONHECIMENTOS ESPECIFICOS", 30,
                ["Procedimentos Policiais", "Direito Administrativo", "Direito Constitucional", "Direito Penal", "Legislação Federal", "Estatuto das Guardas Municipais"]),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var edition in NoticeSubjects.Select(row => row.Edition).Distinct())
            {
                migrationBuilder.Sql(InsertEditionRows(edition));
            }

            for (var index = 0; index < NoticeSubjects.Length; index++)
            {
                var number = index + 1;
                var entries = NoticeSubjects[index].Mapping;
                for (var position = 0; position < entries.Length; position++)
                {
                    migrationBuilder.Sql(InsertMappingEntry(number, position + 1, entries[position]));
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var entryIds = string.Join(", ", NoticeSubjects
                .SelectMany((row, index) => row.Mapping.Select((_, position) => $"'{MappingId(index + 1, position + 1)}'")));
            var noticeSubjectIds = string.Join(", ", NoticeSubjects.Select((_, index) => $"'{NoticeSubjectId(index + 1)}'"));

            migrationBuilder.Sql($"DELETE FROM catalog.notice_subject_mappings WHERE id IN ({entryIds});");

            // A notice subject an admin gave an entry of their own stays: the cascade would take the entry with it.
            migrationBuilder.Sql(
                $"""
                DELETE FROM catalog.notice_subjects
                WHERE id IN ({noticeSubjectIds})
                  AND NOT EXISTS (SELECT 1 FROM catalog.notice_subject_mappings m WHERE m.notice_subject_id = notice_subjects.id);
                """);
        }

        // BR3: one statement per edition, so the "holds no notice subject" check reads the table as it was before
        // the first row of the edition went in. Deleted rows count, and so does a deleted edition.
        private static string InsertEditionRows(int edition)
        {
            var editionId = Id(EditionGroup, edition);
            var rows = new List<string>();
            for (var index = 0; index < NoticeSubjects.Length; index++)
            {
                var row = NoticeSubjects[index];
                if (row.Edition != edition)
                {
                    continue;
                }

                var order = rows.Count;
                var questions = row.Questions.ToString(CultureInfo.InvariantCulture);
                rows.Add($"('{NoticeSubjectId(index + 1)}'::uuid, {QuoteOrNull(row.Group)}, {Quote(row.NormalizedGroup)}, {Quote(row.Label)}, {Quote(row.NormalizedLabel)}, {questions}, {order.ToString(CultureInfo.InvariantCulture)})");
            }

            var values = string.Join(", ", rows);

            return $"""
                INSERT INTO catalog.notice_subjects (id, exam_edition_id, group_label, normalized_group, label, normalized_label, question_count, display_order, tenant_id, created_at, is_deleted)
                SELECT v.id, '{editionId}', v.group_label, v.normalized_group, v.label, v.normalized_label, v.question_count, v.display_order, NULL, '{SeededAt}', false
                FROM (VALUES {values}) AS v(id, group_label, normalized_group, label, normalized_label, question_count, display_order)
                WHERE EXISTS (SELECT 1 FROM catalog.exam_editions e WHERE e.id = '{editionId}' AND e.is_deleted = false)
                  AND NOT EXISTS (SELECT 1 FROM catalog.notice_subjects n WHERE n.exam_edition_id = '{editionId}');
                """;
        }

        // BR4, BR5: the entry is written only for a seeded notice subject and only when its target is found and
        // live. The target is the row with the seeded id first, then the live row with the normalized name; a topic
        // is looked up inside its subject. The foreign keys and the unique index never see a skipped entry.
        private static string InsertMappingEntry(int noticeSubjectNumber, int position, string entry)
        {
            var id = MappingId(noticeSubjectNumber, position);
            var noticeSubjectId = NoticeSubjectId(noticeSubjectNumber);

            if (!entry.Contains(TopicSeparator, StringComparison.Ordinal))
            {
                var subject = Subjects[entry];

                return $"""
                    INSERT INTO catalog.notice_subject_mappings (id, notice_subject_id, subject_id, topic_id, tenant_id, created_at, is_deleted)
                    SELECT '{id}', n.id, target.id, NULL, NULL, '{SeededAt}', false
                    FROM catalog.notice_subjects n
                    JOIN LATERAL ({SubjectLookup(subject)}) AS target ON NOT target.is_deleted
                    WHERE n.id = '{noticeSubjectId}';
                    """;
            }

            var topic = Topics[entry];
            var subjectName = entry[..entry.IndexOf(TopicSeparator, StringComparison.Ordinal)];
            var parent = Subjects[subjectName];
            var topicId = Id(TopicGroup, topic.Number);

            return $"""
                INSERT INTO catalog.notice_subject_mappings (id, notice_subject_id, subject_id, topic_id, tenant_id, created_at, is_deleted)
                SELECT '{id}', n.id, NULL, target.id, NULL, '{SeededAt}', false
                FROM catalog.notice_subjects n
                JOIN LATERAL (
                    SELECT t.id, t.is_deleted FROM catalog.topics t
                    WHERE t.tenant_id IS NULL
                      AND (t.id = '{topicId}'
                           OR (t.normalized_name = {Quote(topic.NormalizedName)}
                               AND t.subject_id = (SELECT p.id FROM ({SubjectLookup(parent)}) AS p)))
                    ORDER BY (t.id = '{topicId}') DESC, t.is_deleted
                    LIMIT 1) AS target ON NOT target.is_deleted
                WHERE n.id = '{noticeSubjectId}';
                """;
        }

        private static string SubjectLookup(CanonicalSubject subject)
        {
            var seeded = Id(SubjectGroup, subject.Number);

            return $"""
                SELECT s.id, s.is_deleted FROM catalog.subjects s
                    WHERE s.tenant_id IS NULL AND (s.id = '{seeded}' OR s.normalized_name = {Quote(subject.NormalizedName)})
                    ORDER BY (s.id = '{seeded}') DESC, s.is_deleted
                    LIMIT 1
                """;
        }

        private static Guid Id(int group, int number) =>
            new($"0198f370-{group:D4}-7000-8000-{number:D12}");

        private static Guid NoticeSubjectId(int number) => Id(NoticeSubjectGroup, number);

        private static Guid MappingId(int noticeSubjectNumber, int position) => Id(MappingGroup, noticeSubjectNumber * 100 + position);

        private static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

        private static string QuoteOrNull(string value) => value is null ? "NULL" : Quote(value);

        private sealed record CanonicalSubject(int Number, string NormalizedName);

        private sealed record CanonicalTopic(int Number, string NormalizedName);

        private sealed record SeedNoticeSubject(int Edition, string Group, string NormalizedGroup, string Label, string NormalizedLabel, int Questions, string[] Mapping);
    }
}
