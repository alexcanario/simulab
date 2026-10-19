using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-78: the notice subjects and their mapping the <c>SeedGuardNoticeSubjects</c> data migration writes for the
/// four municipal guard editions, checked in a real PostgreSQL database. Each test gets its own database, because
/// several of them write rows before or after the seed.
/// </summary>
public sealed class SeedGuardNoticeSubjectsTests
{
    private const string PreviousMigration = "20261014160521_AddSubjectTopicAliases";
    private const string BeforeTaxonomySeed = "20261004210926_AddNoticeSubjects";
    private const string Schema = CatalogModuleDbContext.SchemaName;
    private const string TopicSeparator = " › ";

    private static readonly Guid Curitiba = EditionId(1);
    private static readonly Guid Manaus = EditionId(2);
    private static readonly Guid Salvador = EditionId(3);
    private static readonly Guid Maceio = EditionId(4);

    private const string ModuleOne = "Módulo I - Conhecimentos Básicos";
    private const string ModuleTwo = "Módulo II - Conhecimentos Específicos";

    // The approved list of F-78, written here on its own so a slip in the migration shows up. A mapping entry is
    // a subject name (the whole subject) or "Subject › Topic".
    private static readonly Dictionary<Guid, (int Total, Row[] Rows)> Expected = new()
    {
        [Curitiba] = (40,
        [
            new(null, "Língua Portuguesa", 5,
                ["Leitura e Interpretação", "Morfologia", "Sintaxe", "Ortografia e Pontuação", "Semântica e Léxico"]),
            new(null, "Raciocínio Lógico e Matemático", 5,
                ["Aritmética e Proporcionalidade", "Álgebra e Conjuntos", "Geometria", "Estatística e Tratamento de Informações", "Lógica Proposicional", "Raciocínio Analítico"]),
            new(null, "Noções de Informática", 5, ["Informática"]),
            new(null, "História e Geografia de Curitiba", 5, ["Conhecimentos locais › Curitiba (PR)"]),
            new(null, "Noções de Direito (Constitucional, Administrativo, Penal e Processual Penal)", 10,
                ["Direito Administrativo", "Direito Constitucional", "Direito Penal"]),
            new(null, "Legislação", 10,
                ["Legislação Municipal", "Estatuto das Guardas Municipais", "Estatuto do Desarmamento", "Legislação Federal",
                 "Direito Administrativo › Legislação administrativa", "Direito Penal › Crimes específicos",
                 "Direito Constitucional › Direitos fundamentais", "Procedimentos Policiais › Progressividade da força"]),
        ]),
        [Manaus] = (60,
        [
            new(null, "Língua Portuguesa", 10,
                ["Leitura e Interpretação", "Produção Textual › Comunicação escrita", "Morfologia", "Sintaxe", "Ortografia e Pontuação", "Semântica e Léxico"]),
            new(null, "Informática", 5, ["Informática"]),
            new(null, "Geografia e História de Manaus", 5, ["Conhecimentos locais › Manaus (AM)"]),
            new(null, "Ética e Direitos Humanos", 5, ["Direitos Humanos"]),
            new(null, "Direito Constitucional", 5, ["Direito Constitucional"]),
            new(null, "Direito Penal", 5, ["Direito Penal"]),
            new(null, "Direito Processual Penal", 5, ["Direito Penal › Procedimentos processuais", "Direito Penal › Processo penal"]),
            new(null, "Legislação de trânsito", 5, ["Legislação Federal › Código de Trânsito"]),
            new(null, "Legislação específica", 15,
                ["Estatuto das Guardas Municipais", "Estatuto do Desarmamento", "Legislação Municipal", "Conhecimentos locais › Manaus (AM)",
                 "Legislação Federal › ECA (Lei 8.069/1990)", "Legislação Federal › Lei Maria da Penha (Lei 11.340/2006)",
                 "Direito Administrativo › Legislação administrativa", "Direito Penal › Crimes específicos"]),
        ]),
        [Salvador] = (70,
        [
            new(ModuleOne, "Língua Portuguesa", 10,
                ["Leitura e Interpretação", "Produção Textual", "Morfologia", "Sintaxe", "Ortografia e Pontuação", "Semântica e Léxico"]),
            new(ModuleOne, "Raciocínio Lógico-Matemático", 10,
                ["Aritmética e Proporcionalidade", "Álgebra e Conjuntos", "Geometria", "Estatística e Tratamento de Informações", "Lógica Proposicional", "Raciocínio Analítico"]),
            new(ModuleOne, "Informática", 8, ["Informática"]),
            new(ModuleTwo, "Noções de Direito Constitucional e Direito Civil", 10, ["Direito Constitucional"]),
            new(ModuleTwo, "Noções de Direito Penal e Direito Processual Penal", 7, ["Direito Penal"]),
            new(ModuleTwo, "Noções de Administração e Políticas Públicas", 5, []),
            new(ModuleTwo, "Conhecimentos na Área de Atuação", 10, ["Procedimentos Policiais"]),
            new(ModuleTwo, "Legislação", 10,
                ["Legislação Federal", "Estatuto do Desarmamento", "Estatuto das Guardas Municipais", "Legislação Municipal",
                 "Conhecimentos locais › Salvador (BA)", "Direito Penal › Crimes específicos"]),
        ]),
        [Maceio] = (60,
        [
            new(null, "Língua Portuguesa", 10,
                ["Leitura e Interpretação", "Morfologia", "Sintaxe", "Ortografia e Pontuação", "Semântica e Léxico"]),
            new(null, "Raciocínio Lógico", 5,
                ["Aritmética e Proporcionalidade", "Álgebra e Conjuntos", "Geometria", "Estatística e Tratamento de Informações", "Lógica Proposicional", "Raciocínio Analítico"]),
            new(null, "Noções de Informática", 5, ["Informática"]),
            new(null, "Legislação Municipal Específica", 10, ["Legislação Municipal", "Conhecimentos locais › Maceió (AL)"]),
            new(null, "Conhecimentos Específicos", 30,
                ["Procedimentos Policiais", "Direito Administrativo", "Direito Constitucional", "Direito Penal", "Legislação Federal", "Estatuto das Guardas Municipais"]),
        ]),
    };

    [Fact]
    public async Task Migrate_EmptyDatabase_EachEditionHoldsExactlyItsListedNoticeSubjectsInOrderWithNoTenant()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects")).Should().Be(28);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects WHERE tenant_id IS NOT NULL OR is_deleted")).Should().Be(0);
        database.Context.Database.HasPendingModelChanges().Should().BeFalse("a data migration changes no model (AC9)");

        foreach (var (editionId, (_, rows)) in Expected)
        {
            var stored = await database.NoticeSubjectsAsync(editionId);

            stored.Select(row => (row.DisplayOrder, row.Group, row.Label, row.QuestionCount))
                .Should().Equal(rows.Select((row, index) => (index, row.Group, row.Label, (int?)row.Questions)), editionId.ToString());
        }
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_TheNoticeSubjectsHaveTheFixedIdsOfTheSeed()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        var number = 0;
        foreach (var editionId in new[] { Curitiba, Manaus, Salvador, Maceio })
        {
            foreach (var stored in await database.NoticeSubjectsAsync(editionId))
            {
                number++;
                stored.Id.Should().Be(new Guid($"0198f370-0008-7000-8000-{number:D12}"), stored.Label);
            }
        }

        number.Should().Be(28);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EachNoticeSubjectHasExactlyItsListedMappingEntriesInOrder()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        var total = 0;
        foreach (var (editionId, (_, rows)) in Expected)
        {
            var stored = await database.NoticeSubjectsAsync(editionId);
            for (var index = 0; index < rows.Length; index++)
            {
                var entries = await database.MappingAsync(stored[index].Id);

                entries.Should().Equal(rows[index].Mapping, $"{editionId} {rows[index].Label}");
                total += entries.Count;
            }
        }

        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subject_mappings WHERE is_deleted OR tenant_id IS NOT NULL")).Should().Be(0);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subject_mappings")).Should().Be(total);
        total.Should().Be(Expected.Values.Sum(edition => edition.Rows.Sum(row => row.Mapping.Length)));
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_TheMappingEntriesHaveTheFixedIdsOfTheSeed()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        var number = 0;
        foreach (var (editionId, (_, rows)) in Expected)
        {
            var stored = await database.NoticeSubjectsAsync(editionId);
            for (var index = 0; index < rows.Length; index++)
            {
                number++;
                var ids = await database.MappingIdsAsync(stored[index].Id);

                ids.Should().Equal(
                    Enumerable.Range(1, rows[index].Mapping.Length).Select(position => new Guid($"0198f370-0009-7000-8000-{number * 100 + position:D12}")),
                    rows[index].Label);
            }
        }
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_TheUncoveredNoticeSubjectHasNoEntryAndEveryOtherOneHasAtLeastOne()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        var withoutEntries = await database.QueryAsync(
            """
            SELECT ns.exam_edition_id::text, ns.label FROM catalog.notice_subjects ns
            WHERE NOT EXISTS (SELECT 1 FROM catalog.notice_subject_mappings m WHERE m.notice_subject_id = ns.id)
            """);

        withoutEntries.Should().ContainSingle().Which.Should().Equal(Salvador.ToString(), "Noções de Administração e Políticas Públicas");
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EachEditionsQuestionsAddUpToTheNoticeTotal()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        foreach (var (editionId, (total, _)) in Expected)
        {
            var sum = await database.ScalarAsync($"SELECT sum(question_count) FROM catalog.notice_subjects WHERE exam_edition_id = '{editionId}'");

            sum.Should().Be(total, editionId.ToString());
        }
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EverySeededRowEqualsWhatTheDomainComputes()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();
        var context = database.Context;

        var subjects = await context.Set<NoticeSubject>().AsNoTracking().ToListAsync();
        subjects.Should().HaveCount(28);
        foreach (var stored in subjects)
        {
            var rebuilt = NoticeSubject.Create(stored.ExamEditionId, stored.Group, stored.Label, stored.QuestionCount);

            rebuilt.IsSuccess.Should().BeTrue(stored.Label);
            rebuilt.Value.Group.Should().Be(stored.Group, stored.Label);
            rebuilt.Value.Label.Should().Be(stored.Label);
            rebuilt.Value.NormalizedGroup.Should().Be(stored.NormalizedGroup, stored.Label);
            rebuilt.Value.NormalizedLabel.Should().Be(stored.NormalizedLabel, stored.Label);
            stored.NormalizedGroup.Should().Be(CatalogText.Normalize(stored.Group));
            stored.NormalizedLabel.Should().Be(CatalogText.Normalize(stored.Label));
        }

        var topicSubjects = await context.Set<Topic>().AsNoTracking().ToDictionaryAsync(topic => topic.Id, topic => topic.SubjectId);
        foreach (var stored in subjects)
        {
            var entries = await context.Set<NoticeSubjectMapping>().AsNoTracking()
                .Where(mapping => mapping.NoticeSubjectId == stored.Id)
                .Select(mapping => new NoticeSubjectMappingRequest(mapping.SubjectId, mapping.TopicId))
                .ToListAsync();

            var normalized = NoticeSubjectMappingRules.Normalize(entries);
            normalized.IsSuccess.Should().BeTrue(stored.Label);
            normalized.Value.Should().HaveCount(entries.Count, stored.Label);
            NoticeSubjectMappingRules.CheckOverlap(normalized.Value, topicSubjects, []).IsSuccess.Should().BeTrue(stored.Label);
        }
    }

    [Fact]
    public async Task Migrate_ASalvadorNoticeSubjectTypedByAnAdmin_KeepsOnlyThatRowAndSeedsTheOtherThreeEditions()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(PreviousMigration);
        await database.InsertNoticeSubjectAsync(Salvador, "Matéria do administrador", deleted: false);

        await database.MigrateAsync();

        (await database.NoticeSubjectsAsync(Salvador)).Select(row => row.Label).Should().Equal("Matéria do administrador");
        (await database.ScalarAsync(
            $"SELECT count(*) FROM catalog.notice_subject_mappings m JOIN catalog.notice_subjects ns ON ns.id = m.notice_subject_id WHERE ns.exam_edition_id = '{Salvador}'"))
            .Should().Be(0);
        (await database.NoticeSubjectsAsync(Curitiba)).Should().HaveCount(6);
        (await database.NoticeSubjectsAsync(Manaus)).Should().HaveCount(9);
        (await database.NoticeSubjectsAsync(Maceio)).Should().HaveCount(5);
    }

    [Fact]
    public async Task Migrate_AManausNoticeSubjectAlreadyDeleted_SkipsTheWholeEditionToo()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(PreviousMigration);
        await database.InsertNoticeSubjectAsync(Manaus, "Matéria apagada", deleted: true);

        await database.MigrateAsync();

        (await database.ScalarAsync($"SELECT count(*) FROM catalog.notice_subjects WHERE exam_edition_id = '{Manaus}'")).Should().Be(1);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects")).Should().Be(1 + 6 + 8 + 5);
        (await database.ScalarAsync(
            $"SELECT count(*) FROM catalog.notice_subject_mappings m JOIN catalog.notice_subjects ns ON ns.id = m.notice_subject_id WHERE ns.exam_edition_id = '{Manaus}'"))
            .Should().Be(0);
    }

    [Fact]
    public async Task Migrate_TheMaceioEditionSoftDeletedBeforehand_GetsNoNoticeSubjectAndTheMigrationSucceeds()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(PreviousMigration);
        await database.ExecuteAsync($"UPDATE catalog.exam_editions SET is_deleted = true, deleted_at = now() WHERE id = '{Maceio}'");

        await database.MigrateAsync();

        (await database.ScalarAsync($"SELECT count(*) FROM catalog.notice_subjects WHERE exam_edition_id = '{Maceio}'")).Should().Be(0);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects")).Should().Be(6 + 9 + 8);
    }

    [Fact]
    public async Task Migrate_ACanonicalSubjectUnderAnotherId_IsStillMappedByItsName()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(BeforeTaxonomySeed);
        var reused = await database.InsertSubjectAsync("Informática");

        await database.MigrateAsync();

        (await database.ScalarAsync("SELECT count(*) FROM catalog.subjects WHERE normalized_name = 'INFORMATICA'")).Should().Be(1);
        var informatics = await database.QueryAsync(
            "SELECT DISTINCT m.subject_id::text FROM catalog.notice_subject_mappings m JOIN catalog.subjects s ON s.id = m.subject_id WHERE s.normalized_name = 'INFORMATICA'");
        informatics.Should().ContainSingle().Which.Should().Equal(reused.ToString());
        (await database.ScalarAsync(
            $"SELECT count(*) FROM catalog.notice_subject_mappings WHERE subject_id = '{reused}'")).Should().Be(4);
    }

    [Fact]
    public async Task Migrate_ASoftDeletedTopic_IsSkippedAndTheRowIsSeededWithTheEntriesLeft()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(PreviousMigration);
        await database.ExecuteAsync("UPDATE catalog.topics SET is_deleted = true, deleted_at = now() WHERE normalized_name = 'CURITIBA (PR)'");

        await database.MigrateAsync();

        var stored = (await database.NoticeSubjectsAsync(Curitiba))[3];
        stored.Label.Should().Be("História e Geografia de Curitiba");
        (await database.MappingAsync(stored.Id)).Should().BeEmpty();
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects")).Should().Be(28);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subject_mappings")).Should().Be(
            Expected.Values.Sum(edition => edition.Rows.Sum(row => row.Mapping.Length)) - 1);
    }

    [Fact]
    public async Task Migrate_ASoftDeletedSubject_IsSkippedWholeAndItsTopicsToo()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(PreviousMigration);
        await database.ExecuteAsync("UPDATE catalog.subjects SET is_deleted = true, deleted_at = now() WHERE normalized_name = 'GEOMETRIA'");

        await database.MigrateAsync();

        (await database.ScalarAsync(
            "SELECT count(*) FROM catalog.notice_subject_mappings m JOIN catalog.subjects s ON s.id = m.subject_id WHERE s.normalized_name = 'GEOMETRIA'"))
            .Should().Be(0);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects")).Should().Be(28);
    }

    [Fact]
    public async Task Migrate_AgainAfterAnAdminEditedASeededRow_KeepsTheEditAndAddsNoDuplicate()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();
        await database.ExecuteAsync("UPDATE catalog.notice_subjects SET label = 'Português', normalized_label = 'PORTUGUES' WHERE id = '0198f370-0008-7000-8000-000000000001'");

        await database.MigrateAsync();

        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects")).Should().Be(28);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects WHERE label = 'Português'")).Should().Be(1);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subject_mappings")).Should().Be(
            Expected.Values.Sum(edition => edition.Rows.Sum(row => row.Mapping.Length)));
    }

    [Fact]
    public async Task Down_RemovesTheSeededRowsAndKeepsTheAdminsOwnNoticeSubject()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();
        await database.InsertNoticeSubjectAsync(Curitiba, "Matéria do administrador", deleted: false);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects")).Should().Be(29);

        await database.MigrateAsync(PreviousMigration);

        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subject_mappings")).Should().Be(0);
        (await database.NoticeSubjectsAsync(Curitiba)).Select(row => row.Label).Should().Equal("Matéria do administrador");
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects")).Should().Be(1);
    }

    [Fact]
    public async Task Down_KeepsASeededNoticeSubjectThatHoldsAnEntryOfTheAdmin()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();
        var geometry = await database.ScalarGuidAsync("SELECT id FROM catalog.subjects WHERE normalized_name = 'GEOMETRIA'");
        var seeded = new Guid("0198f370-0008-7000-8000-000000000003");
        await database.InsertMappingAsync(seeded, geometry);

        await database.MigrateAsync(PreviousMigration);

        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subjects")).Should().Be(1);
        (await database.ScalarAsync($"SELECT count(*) FROM catalog.notice_subjects WHERE id = '{seeded}'")).Should().Be(1);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.notice_subject_mappings")).Should().Be(1);
    }

    private static Guid EditionId(int number) => new($"0198f370-0004-7000-8000-{number:D12}");

    private static async Task<Database> DatabaseAsync()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(SeedGuardNoticeSubjectsTests) + Guid.CreateVersion7().ToString("N"));
        var context = new CatalogModuleDbContext(
            new DbContextOptionsBuilder<CatalogModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());

        return new Database(context, connectionString);
    }

    private sealed record Row(string? Group, string Label, int Questions, string[] Mapping);

    private sealed record StoredNoticeSubject(Guid Id, int DisplayOrder, string? Group, string Label, int? QuestionCount);

    private sealed class Database(CatalogModuleDbContext context, string connectionString) : IAsyncDisposable
    {
        public CatalogModuleDbContext Context { get; } = context;

        public Task MigrateAsync(string? target = null) => Context.GetService<IMigrator>().MigrateAsync(target);

        public async Task<Guid> InsertSubjectAsync(string name)
        {
            var id = Guid.CreateVersion7();
            await ExecuteAsync(
                "INSERT INTO catalog.subjects (id, name, normalized_name, tenant_id, created_at, is_deleted) VALUES (@id, @name, @normalized, NULL, now(), false)",
                ("id", id), ("name", name), ("normalized", CatalogText.Normalize(name)));

            return id;
        }

        public Task InsertNoticeSubjectAsync(Guid editionId, string label, bool deleted) =>
            ExecuteAsync(
                """
                INSERT INTO catalog.notice_subjects (id, exam_edition_id, normalized_group, label, normalized_label, display_order, tenant_id, created_at, is_deleted)
                VALUES (@id, @edition, '', @label, @normalized, 1, NULL, now(), @deleted)
                """,
                ("id", Guid.CreateVersion7()), ("edition", editionId), ("label", label), ("normalized", CatalogText.Normalize(label)), ("deleted", deleted));

        public Task InsertMappingAsync(Guid noticeSubjectId, Guid subjectId) =>
            ExecuteAsync(
                "INSERT INTO catalog.notice_subject_mappings (id, notice_subject_id, subject_id, tenant_id, created_at, is_deleted) VALUES (@id, @notice, @subject, NULL, now(), false)",
                ("id", Guid.CreateVersion7()), ("notice", noticeSubjectId), ("subject", subjectId));

        public async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task<long> ScalarAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        }

        public async Task<Guid> ScalarGuidAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            return (Guid)(await command.ExecuteScalarAsync())!;
        }

        /// <summary>Every row of the query as its columns read as text; a null column is an empty string.</summary>
        public async Task<List<string[]>> QueryAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string[]>();
            while (await reader.ReadAsync())
            {
                var row = new string[reader.FieldCount];
                for (var i = 0; i < row.Length; i++)
                {
                    row[i] = reader.IsDBNull(i) ? string.Empty : reader.GetString(i);
                }

                rows.Add(row);
            }

            return rows;
        }

        /// <summary>The live notice subjects of one edition in their order, the way the edition page lists them.</summary>
        public async Task<List<StoredNoticeSubject>> NoticeSubjectsAsync(Guid editionId)
        {
            var rows = await QueryAsync(
                $"""
                SELECT id::text, display_order::text, group_label, label, question_count::text
                FROM catalog.notice_subjects
                WHERE exam_edition_id = '{editionId}' AND NOT is_deleted
                ORDER BY display_order
                """);

            return rows
                .Select(row => new StoredNoticeSubject(
                    Guid.Parse(row[0]),
                    int.Parse(row[1], CultureInfo.InvariantCulture),
                    row[2].Length == 0 ? null : row[2],
                    row[3],
                    row[4].Length == 0 ? null : int.Parse(row[4], CultureInfo.InvariantCulture)))
                .ToList();
        }

        /// <summary>The live entries of a notice subject as "Subject" or "Subject › Topic", in the order of their ids.</summary>
        public async Task<List<string>> MappingAsync(Guid noticeSubjectId)
        {
            var rows = await QueryAsync(
                $"""
                SELECT coalesce(s.name, ts.name || '{TopicSeparator}' || t.name)
                FROM catalog.notice_subject_mappings m
                LEFT JOIN catalog.subjects s ON s.id = m.subject_id
                LEFT JOIN catalog.topics t ON t.id = m.topic_id
                LEFT JOIN catalog.subjects ts ON ts.id = t.subject_id
                WHERE m.notice_subject_id = '{noticeSubjectId}' AND NOT m.is_deleted
                ORDER BY m.id
                """);

            return rows.Select(row => row[0]).ToList();
        }

        public async Task<List<Guid>> MappingIdsAsync(Guid noticeSubjectId)
        {
            var rows = await QueryAsync($"SELECT id::text FROM catalog.notice_subject_mappings WHERE notice_subject_id = '{noticeSubjectId}' ORDER BY id");

            return rows.Select(row => Guid.Parse(row[0])).ToList();
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
