using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// F-37: the municipal guard catalog every environment starts with — seven exam boards, six issuing
    /// authorities, six exams and four editions. A data migration, so it runs once per database and an
    /// admin's edit or delete stays final (BR1, BR5). The values are frozen literals, normalized columns
    /// included, never computed from code that evolves (BR4); a test ties them to the domain rules.
    /// </summary>
    public partial class SeedMunicipalGuardCatalog : Migration
    {
        private static readonly DateTimeOffset SeededAt = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

        private static readonly Guid[] OrganizerIds =
        [
            new("0198f370-0001-7000-8000-000000000001"),
            new("0198f370-0001-7000-8000-000000000002"),
            new("0198f370-0001-7000-8000-000000000003"),
            new("0198f370-0001-7000-8000-000000000004"),
            new("0198f370-0001-7000-8000-000000000005"),
            new("0198f370-0001-7000-8000-000000000006"),
            new("0198f370-0001-7000-8000-000000000007"),
        ];

        private static readonly Guid[] AuthorityIds =
        [
            new("0198f370-0002-7000-8000-000000000001"),
            new("0198f370-0002-7000-8000-000000000002"),
            new("0198f370-0002-7000-8000-000000000003"),
            new("0198f370-0002-7000-8000-000000000004"),
            new("0198f370-0002-7000-8000-000000000005"),
            new("0198f370-0002-7000-8000-000000000006"),
        ];

        private static readonly Guid[] ExamIds =
        [
            new("0198f370-0003-7000-8000-000000000001"),
            new("0198f370-0003-7000-8000-000000000002"),
            new("0198f370-0003-7000-8000-000000000003"),
            new("0198f370-0003-7000-8000-000000000004"),
            new("0198f370-0003-7000-8000-000000000005"),
            new("0198f370-0003-7000-8000-000000000006"),
        ];

        private static readonly Guid[] EditionIds =
        [
            new("0198f370-0004-7000-8000-000000000001"),
            new("0198f370-0004-7000-8000-000000000002"),
            new("0198f370-0004-7000-8000-000000000003"),
            new("0198f370-0004-7000-8000-000000000004"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Name, acronym, normalized name, normalized acronym (Simulae's seven boards, BR6).
            string[][] organizers =
            [
                ["Instituto AOCP", "AOCP", "INSTITUTO AOCP", "AOCP"],
                ["Instituto Consulplan", "CONSULPLAN", "INSTITUTO CONSULPLAN", "CONSULPLAN"],
                ["Fundação Getulio Vargas (FGV)", "FGV", "FUNDACAO GETULIO VARGAS (FGV)", "FGV"],
                ["Copeve/Ufal", "COPEVE/UFAL", "COPEVE/UFAL", "COPEVE/UFAL"],
                ["Vunesp", "VUNESP", "VUNESP", "VUNESP"],
                ["FCC", "FCC", "FCC", "FCC"],
                ["Cebraspe", "CEBRASPE", "CEBRASPE", "CEBRASPE"],
            ];

            for (var i = 0; i < organizers.Length; i++)
            {
                migrationBuilder.InsertData(
                    schema: "catalog",
                    table: "organizers",
                    columns:
                    [
                        "id", "name", "acronym", "kind", "normalized_name", "normalized_acronym",
                        "created_at", "is_deleted",
                    ],
                    values: new object[]
                    {
                        OrganizerIds[i], organizers[i][0], organizers[i][1], "ExamBoard", organizers[i][2], organizers[i][3],
                        SeededAt, false,
                    });
            }

            // Name, acronym, normalized name, normalized acronym (one city hall per city, BR7).
            string[][] authorities =
            [
                ["Prefeitura de Curitiba", "PM-CURITIBA", "PREFEITURA DE CURITIBA", "PM-CURITIBA"],
                ["Prefeitura de Manaus", "PM-MANAUS", "PREFEITURA DE MANAUS", "PM-MANAUS"],
                ["Prefeitura de Salvador", "PM-SALVADOR", "PREFEITURA DE SALVADOR", "PM-SALVADOR"],
                ["Prefeitura do Recife", "PM-RECIFE", "PREFEITURA DO RECIFE", "PM-RECIFE"],
                ["Prefeitura de Goiânia", "PM-GOIANIA", "PREFEITURA DE GOIANIA", "PM-GOIANIA"],
                ["Prefeitura de Maceió", "PM-MACEIO", "PREFEITURA DE MACEIO", "PM-MACEIO"],
            ];

            for (var i = 0; i < authorities.Length; i++)
            {
                migrationBuilder.InsertData(
                    schema: "catalog",
                    table: "issuing_authorities",
                    columns:
                    [
                        "id", "name", "acronym", "normalized_name", "normalized_acronym",
                        "created_at", "is_deleted",
                    ],
                    values: new object[]
                    {
                        AuthorityIds[i], authorities[i][0], authorities[i][1], authorities[i][2], authorities[i][3],
                        SeededAt, false,
                    });
            }

            // Name, scope detail, normalized name, normalized scope detail (BR8). The i-th exam belongs to the
            // i-th authority.
            string[][] exams =
            [
                ["Guarda Municipal", "Curitiba", "GUARDA MUNICIPAL", "CURITIBA"],
                ["Guarda Municipal", "Manaus", "GUARDA MUNICIPAL", "MANAUS"],
                ["Guarda Civil Municipal", "Salvador", "GUARDA CIVIL MUNICIPAL", "SALVADOR"],
                ["Guarda Municipal", "Recife", "GUARDA MUNICIPAL", "RECIFE"],
                ["Guarda Municipal", "Goiânia", "GUARDA MUNICIPAL", "GOIANIA"],
                ["Guarda Civil Municipal", "Maceió", "GUARDA CIVIL MUNICIPAL", "MACEIO"],
            ];

            for (var i = 0; i < exams.Length; i++)
            {
                migrationBuilder.InsertData(
                    schema: "catalog",
                    table: "exams",
                    columns:
                    [
                        "id", "issuing_authority_id", "name", "assessment_type", "scope", "scope_detail", "content_language",
                        "normalized_name", "normalized_scope_detail", "created_at", "is_deleted",
                    ],
                    values: new object[]
                    {
                        ExamIds[i], AuthorityIds[i], exams[i][0], "PublicServiceExam", "Municipal", exams[i][1], "pt-BR",
                        exams[i][2], exams[i][3], SeededAt, false,
                    });
            }

            // Exam index (Curitiba, Manaus, Salvador, Maceió), board index, year, position, normalized position,
            // notice reference, notice link (BR10, BR11). No application date: no primary source confirmed one.
            (int Exam, int Board, int Year, string Position, string NormalizedPosition, string Reference, string Url)[] editions =
            [
                (0, 0, 2025, "Guarda Municipal", "GUARDA MUNICIPAL", "Edital nº 02/2025",
                    "https://www.institutoaocp.org.br/concursos/669"),
                (1, 1, 2026, "Técnico Municipal I - Guarda Municipal", "TECNICO MUNICIPAL I - GUARDA MUNICIPAL",
                    "Edital nº 01, de 23 de março de 2026",
                    "https://dhg1h5j42swfq.cloudfront.net/2026/03/24001950/edital-gcm-manaus-2026.pdf"),
                (2, 2, 2026, "Guarda Civil Municipal", "GUARDA CIVIL MUNICIPAL", "Edital nº 02/2026",
                    "https://conhecimento.fgv.br/concursos/pmsguarda2026"),
                (5, 3, 2026, "Guarda Civil Municipal", "GUARDA CIVIL MUNICIPAL", "Edital nº 01/2026",
                    "https://maceio.al.gov.br/noticias/semsc/prefeitura-de-maceio-publica-edital-de-concurso-para-a-guarda-civil-municipal"),
            ];

            for (var i = 0; i < editions.Length; i++)
            {
                var edition = editions[i];
                migrationBuilder.InsertData(
                    schema: "catalog",
                    table: "exam_editions",
                    columns:
                    [
                        "id", "exam_id", "organizer_id", "notice_year", "position", "normalized_position",
                        "notice_reference", "notice_url", "status", "created_at", "is_deleted",
                    ],
                    values: new object[]
                    {
                        EditionIds[i], ExamIds[edition.Exam], OrganizerIds[edition.Board], edition.Year, edition.Position,
                        edition.NormalizedPosition, edition.Reference, edition.Url, "Published", SeededAt, false,
                    });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DeleteAll(migrationBuilder, "exam_editions", EditionIds);
            DeleteAll(migrationBuilder, "exams", ExamIds);
            DeleteAll(migrationBuilder, "issuing_authorities", AuthorityIds);
            DeleteAll(migrationBuilder, "organizers", OrganizerIds);
        }

        private static void DeleteAll(MigrationBuilder migrationBuilder, string table, Guid[] ids)
        {
            foreach (var id in ids)
            {
                migrationBuilder.DeleteData(schema: "catalog", table: table, keyColumn: "id", keyValue: id);
            }
        }
    }
}
