using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// F-51: the municipal guard subject taxonomy every environment starts with — 23 subjects and 70 topics from
    /// Simulae's guard seed, remapped onto the fixed areas of F-79. A data migration, so it runs once per database
    /// and an admin's edit, move or delete stays final (BR1, BR7). The values are frozen literals, normalized
    /// columns included, never computed from code that evolves (BR2); a test ties them to the domain rules.
    /// A name already taken is reused, never an error (BR6): the inserts are SQL that skips a taken name, because
    /// <c>InsertData</c> cannot skip a conflict.
    /// </summary>
    public partial class SeedGuardTaxonomy : Migration
    {
        private const int SubjectGroup = 6;
        private const int TopicGroup = 7;
        private const string SeededAt = "2026-10-05T12:00:00+00";

        // The order and the numbers are frozen: a number is the tail of the row's fixed id.
        private static readonly SeedSubject[] Subjects =
        [
            new(1, "Leitura e Interpretação", "LEITURA E INTERPRETACAO", "Languages",
            [
                new(1, "Interpretação de textos", "INTERPRETACAO DE TEXTOS"),
            ]),
            new(2, "Produção Textual", "PRODUCAO TEXTUAL", "Languages",
            [
                new(2, "Redação (nível superior)", "REDACAO (NIVEL SUPERIOR)"),
                new(3, "Comunicação escrita", "COMUNICACAO ESCRITA"),
            ]),
            new(3, "Morfologia", "MORFOLOGIA", "Languages",
            [
                new(4, "Morfologia", "MORFOLOGIA"),
            ]),
            new(4, "Sintaxe", "SINTAXE", "Languages",
            [
                new(5, "Sintaxe, Concordância e Regência", "SINTAXE, CONCORDANCIA E REGENCIA"),
            ]),
            new(5, "Ortografia e Pontuação", "ORTOGRAFIA E PONTUACAO", "Languages",
            [
                new(6, "Ortografia", "ORTOGRAFIA"),
                new(7, "Pontuação", "PONTUACAO"),
            ]),
            new(6, "Semântica e Léxico", "SEMANTICA E LEXICO", "Languages",
            [
                new(8, "Semântica", "SEMANTICA"),
                new(9, "Vocabulário", "VOCABULARIO"),
            ]),
            new(7, "Aritmética e Proporcionalidade", "ARITMETICA E PROPORCIONALIDADE", "Mathematics",
            [
                new(10, "Operações básicas", "OPERACOES BASICAS"),
                new(11, "Percentuais", "PERCENTUAIS"),
                new(12, "Regra de três", "REGRA DE TRES"),
                new(13, "Problemas práticos", "PROBLEMAS PRATICOS"),
            ]),
            new(8, "Álgebra e Conjuntos", "ALGEBRA E CONJUNTOS", "Mathematics",
            [
                new(14, "Equações algébricas", "EQUACOES ALGEBRICAS"),
                new(15, "Teoria dos conjuntos", "TEORIA DOS CONJUNTOS"),
                new(16, "Análise combinatória", "ANALISE COMBINATORIA"),
            ]),
            new(9, "Geometria", "GEOMETRIA", "Mathematics",
            [
                new(17, "Geometria", "GEOMETRIA"),
            ]),
            new(10, "Estatística e Tratamento de Informações", "ESTATISTICA E TRATAMENTO DE INFORMACOES", "Mathematics",
            [
                new(18, "Tratamento de informações e estatística", "TRATAMENTO DE INFORMACOES E ESTATISTICA"),
            ]),
            new(11, "Lógica Proposicional", "LOGICA PROPOSICIONAL", "LogicalReasoning",
            [
                new(19, "Lógica proposicional", "LOGICA PROPOSICIONAL"),
                new(20, "Lógica formal", "LOGICA FORMAL"),
            ]),
            new(12, "Raciocínio Analítico", "RACIOCINIO ANALITICO", "LogicalReasoning",
            [
                new(21, "Sequências lógicas", "SEQUENCIAS LOGICAS"),
                new(22, "Análise de dados", "ANALISE DE DADOS"),
                new(23, "Análise crítica", "ANALISE CRITICA"),
            ]),
            new(13, "Informática", "INFORMATICA", "InformationTechnology",
            [
                new(24, "Sistemas operacionais Windows", "SISTEMAS OPERACIONAIS WINDOWS"),
                new(25, "Pacotes Microsoft Office", "PACOTES MICROSOFT OFFICE"),
                new(26, "LibreOffice", "LIBREOFFICE"),
                new(27, "Segurança na internet", "SEGURANCA NA INTERNET"),
                new(28, "Conceitos de hardware", "CONCEITOS DE HARDWARE"),
            ]),
            new(14, "Direito Administrativo", "DIREITO ADMINISTRATIVO", "Law",
            [
                new(29, "Princípios da administração pública", "PRINCIPIOS DA ADMINISTRACAO PUBLICA"),
                new(30, "Hierarquia administrativa", "HIERARQUIA ADMINISTRATIVA"),
                new(31, "Legislação administrativa", "LEGISLACAO ADMINISTRATIVA"),
                new(32, "Conceitos fundamentais", "CONCEITOS FUNDAMENTAIS"),
                new(33, "Atos administrativos", "ATOS ADMINISTRATIVOS"),
                new(34, "Contratos administrativos", "CONTRATOS ADMINISTRATIVOS"),
            ]),
            new(15, "Direito Constitucional", "DIREITO CONSTITUCIONAL", "Law",
            [
                new(35, "Direitos fundamentais", "DIREITOS FUNDAMENTAIS"),
                new(36, "Constituição Federal (art. 144)", "CONSTITUICAO FEDERAL (ART. 144)"),
                new(37, "Legislação de proteção", "LEGISLACAO DE PROTECAO"),
                new(38, "Princípios constitucionais", "PRINCIPIOS CONSTITUCIONAIS"),
            ]),
            new(16, "Direito Penal", "DIREITO PENAL", "Law",
            [
                new(39, "Tipificação de crimes", "TIPIFICACAO DE CRIMES"),
                new(40, "Tipos penais", "TIPOS PENAIS"),
                new(41, "Penas e sanções", "PENAS E SANCOES"),
                new(42, "Códigos penais", "CODIGOS PENAIS"),
                new(43, "Crimes específicos", "CRIMES ESPECIFICOS"),
                new(44, "Procedimentos processuais", "PROCEDIMENTOS PROCESSUAIS"),
                new(45, "Processo penal", "PROCESSO PENAL"),
            ]),
            new(17, "Direitos Humanos", "DIREITOS HUMANOS", "Law",
            [
                new(46, "Proteção de direitos fundamentais", "PROTECAO DE DIREITOS FUNDAMENTAIS"),
                new(47, "Legislação de direitos humanos", "LEGISLACAO DE DIREITOS HUMANOS"),
                new(48, "Direitos humanos e cidadania", "DIREITOS HUMANOS E CIDADANIA"),
                new(49, "Cidadania", "CIDADANIA"),
            ]),
            new(18, "Estatuto do Desarmamento", "ESTATUTO DO DESARMAMENTO", "Law",
            [
                new(50, "Lei 10.826/2003 (Estatuto do Desarmamento)", "LEI 10.826/2003 (ESTATUTO DO DESARMAMENTO)"),
            ]),
            new(19, "Estatuto das Guardas Municipais", "ESTATUTO DAS GUARDAS MUNICIPAIS", "Law",
            [
                new(51, "Lei 13.022/2014 (Estatuto das Guardas)", "LEI 13.022/2014 (ESTATUTO DAS GUARDAS)"),
            ]),
            new(20, "Legislação Municipal", "LEGISLACAO MUNICIPAL", "Law",
            [
                new(52, "Estatuto dos Funcionários Públicos Municipais", "ESTATUTO DOS FUNCIONARIOS PUBLICOS MUNICIPAIS"),
                new(53, "Lei complementar municipal", "LEI COMPLEMENTAR MUNICIPAL"),
                new(54, "Estatuto de servidores municipais", "ESTATUTO DE SERVIDORES MUNICIPAIS"),
                new(55, "Legislação municipal específica", "LEGISLACAO MUNICIPAL ESPECIFICA"),
                new(56, "Organização municipal", "ORGANIZACAO MUNICIPAL"),
            ]),
            new(21, "Legislação Federal", "LEGISLACAO FEDERAL", "Law",
            [
                new(57, "Código de Trânsito", "CODIGO DE TRANSITO"),
                new(58, "ECA (Lei 8.069/1990)", "ECA (LEI 8.069/1990)"),
                new(59, "Lei Maria da Penha (Lei 11.340/2006)", "LEI MARIA DA PENHA (LEI 11.340/2006)"),
            ]),
            new(22, "Procedimentos Policiais", "PROCEDIMENTOS POLICIAIS", "SpecificKnowledge",
            [
                new(60, "Técnicas de abordagem", "TECNICAS DE ABORDAGEM"),
                new(61, "Progressividade da força", "PROGRESSIVIDADE DA FORCA"),
                new(62, "Controle de distúrbios", "CONTROLE DE DISTURBIOS"),
                new(63, "Manejo de armamento", "MANEJO DE ARMAMENTO"),
                new(64, "Câmeras corporais", "CAMERAS CORPORAIS"),
            ]),
            new(23, "Conhecimentos locais", "CONHECIMENTOS LOCAIS", "SpecificKnowledge",
            [
                new(65, "Curitiba (PR)", "CURITIBA (PR)"),
                new(66, "Manaus (AM)", "MANAUS (AM)"),
                new(67, "Salvador (BA)", "SALVADOR (BA)"),
                new(68, "Recife (PE)", "RECIFE (PE)"),
                new(69, "Goiânia (GO)", "GOIANIA (GO)"),
                new(70, "Maceió (AL)", "MACEIO (AL)"),
            ]),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var subject in Subjects)
            {
                // BR6, BR7: a taken normalized name (deleted rows included) is left as it is; the area comes from
                // the fixed list and is never written over an existing row.
                migrationBuilder.Sql(
                    $"""
                    INSERT INTO catalog.subjects (id, name, normalized_name, area_id, tenant_id, created_at, is_deleted)
                    SELECT '{Id(SubjectGroup, subject.Number)}', {Quote(subject.Name)}, {Quote(subject.NormalizedName)},
                           (SELECT a.id FROM catalog.areas a WHERE a.tenant_id IS NULL AND a.code = {Quote(subject.AreaCode)}),
                           NULL, '{SeededAt}', false
                    WHERE NOT EXISTS (
                        SELECT 1 FROM catalog.subjects s
                        WHERE s.tenant_id IS NULL AND s.normalized_name = {Quote(subject.NormalizedName)});
                    """);

                foreach (var topic in subject.Topics)
                {
                    // BR6: the topic goes under the live subject of that name, seeded or already there; under a
                    // deleted subject it is skipped, and a taken topic name in that subject is not repeated.
                    migrationBuilder.Sql(
                        $"""
                        INSERT INTO catalog.topics (id, subject_id, name, normalized_name, tenant_id, created_at, is_deleted)
                        SELECT '{Id(TopicGroup, topic.Number)}', s.id, {Quote(topic.Name)}, {Quote(topic.NormalizedName)},
                               NULL, '{SeededAt}', false
                        FROM catalog.subjects s
                        WHERE s.tenant_id IS NULL AND s.is_deleted = false
                          AND s.normalized_name = {Quote(subject.NormalizedName)}
                          AND NOT EXISTS (
                              SELECT 1 FROM catalog.topics t
                              WHERE t.tenant_id IS NULL AND t.subject_id = s.id
                                AND t.normalized_name = {Quote(topic.NormalizedName)});
                        """);
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var topicIds = string.Join(", ", Subjects.SelectMany(subject => subject.Topics).Select(topic => $"'{Id(TopicGroup, topic.Number)}'"));
            var subjectIds = string.Join(", ", Subjects.Select(subject => $"'{Id(SubjectGroup, subject.Number)}'"));

            migrationBuilder.Sql($"DELETE FROM catalog.topics WHERE id IN ({topicIds});");

            // A subject an admin filled with topics of their own stays: the foreign key keeps it.
            migrationBuilder.Sql(
                $"""
                DELETE FROM catalog.subjects
                WHERE id IN ({subjectIds})
                  AND NOT EXISTS (SELECT 1 FROM catalog.topics t WHERE t.subject_id = subjects.id);
                """);
        }

        private static Guid Id(int group, int number) =>
            new($"0198f370-{group:D4}-7000-8000-{number:D12}");

        private static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

        private sealed record SeedTopic(int Number, string Name, string NormalizedName);

        private sealed record SeedSubject(int Number, string Name, string NormalizedName, string AreaCode, SeedTopic[] Topics);
    }
}
