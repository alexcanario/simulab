using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-51: the guard taxonomy the <c>SeedGuardTaxonomy</c> data migration writes, checked in a real PostgreSQL
/// database. Each test gets its own database, because several of them write rows before or after the seed.
/// </summary>
public sealed class SeedGuardTaxonomyTests
{
    private const string PreviousMigration = "20261004210926_AddNoticeSubjects";
    private const string Schema = CatalogModuleDbContext.SchemaName;

    // The approved list of F-51, written here on its own so a slip in the migration shows up (subject, area, topics).
    private static readonly (string Subject, string Area, string[] Topics)[] Expected =
    [
        ("Leitura e Interpretação", "Languages", ["Interpretação de textos"]),
        ("Produção Textual", "Languages", ["Redação (nível superior)", "Comunicação escrita"]),
        ("Morfologia", "Languages", ["Morfologia"]),
        ("Sintaxe", "Languages", ["Sintaxe, Concordância e Regência"]),
        ("Ortografia e Pontuação", "Languages", ["Ortografia", "Pontuação"]),
        ("Semântica e Léxico", "Languages", ["Semântica", "Vocabulário"]),
        ("Aritmética e Proporcionalidade", "Mathematics", ["Operações básicas", "Percentuais", "Regra de três", "Problemas práticos"]),
        ("Álgebra e Conjuntos", "Mathematics", ["Equações algébricas", "Teoria dos conjuntos", "Análise combinatória"]),
        ("Geometria", "Mathematics", ["Geometria"]),
        ("Estatística e Tratamento de Informações", "Mathematics", ["Tratamento de informações e estatística"]),
        ("Lógica Proposicional", "LogicalReasoning", ["Lógica proposicional", "Lógica formal"]),
        ("Raciocínio Analítico", "LogicalReasoning", ["Sequências lógicas", "Análise de dados", "Análise crítica"]),
        ("Informática", "InformationTechnology",
            ["Sistemas operacionais Windows", "Pacotes Microsoft Office", "LibreOffice", "Segurança na internet", "Conceitos de hardware"]),
        ("Direito Administrativo", "Law",
            ["Princípios da administração pública", "Hierarquia administrativa", "Legislação administrativa", "Conceitos fundamentais", "Atos administrativos", "Contratos administrativos"]),
        ("Direito Constitucional", "Law",
            ["Direitos fundamentais", "Constituição Federal (art. 144)", "Legislação de proteção", "Princípios constitucionais"]),
        ("Direito Penal", "Law",
            ["Tipificação de crimes", "Tipos penais", "Penas e sanções", "Códigos penais", "Crimes específicos", "Procedimentos processuais", "Processo penal"]),
        ("Direitos Humanos", "Law",
            ["Proteção de direitos fundamentais", "Legislação de direitos humanos", "Direitos humanos e cidadania", "Cidadania"]),
        ("Estatuto do Desarmamento", "Law", ["Lei 10.826/2003 (Estatuto do Desarmamento)"]),
        ("Estatuto das Guardas Municipais", "Law", ["Lei 13.022/2014 (Estatuto das Guardas)"]),
        ("Legislação Municipal", "Law",
            ["Estatuto dos Funcionários Públicos Municipais", "Lei complementar municipal", "Estatuto de servidores municipais", "Legislação municipal específica", "Organização municipal"]),
        ("Legislação Federal", "Law", ["Código de Trânsito", "ECA (Lei 8.069/1990)", "Lei Maria da Penha (Lei 11.340/2006)"]),
        ("Procedimentos Policiais", "SpecificKnowledge",
            ["Técnicas de abordagem", "Progressividade da força", "Controle de distúrbios", "Manejo de armamento", "Câmeras corporais"]),
        ("Conhecimentos locais", "SpecificKnowledge",
            ["Curitiba (PR)", "Manaus (AM)", "Salvador (BA)", "Recife (PE)", "Goiânia (GO)", "Maceió (AL)"]),
    ];

    [Fact]
    public async Task Migrate_EmptyDatabase_SeedsTheListedSubjectsAndTopicsWithNoTenant()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        (await database.ScalarAsync("SELECT count(*) FROM catalog.subjects")).Should().Be(23);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.topics")).Should().Be(70);
        (await database.ScalarAsync(
            "SELECT (SELECT count(*) FROM catalog.subjects WHERE tenant_id IS NOT NULL OR is_deleted) + (SELECT count(*) FROM catalog.topics WHERE tenant_id IS NOT NULL OR is_deleted)"))
            .Should().Be(0);

        var seeded = await database.TopicsBySubjectAsync();
        seeded.Keys.Should().BeEquivalentTo(Expected.Select(row => row.Subject));
        foreach (var (subject, _, topics) in Expected)
        {
            seeded[subject].Should().Equal(topics, subject);
        }
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EverySubjectHasItsListedAreaAndTheLocalSubjectHasOnlyTheSixCities()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();

        var areas = await database.QueryAsync("SELECT s.name, a.code FROM catalog.subjects s LEFT JOIN catalog.areas a ON a.id = s.area_id");
        areas.Should().BeEquivalentTo(Expected.Select(row => new[] { row.Subject, row.Area }));

        var local = (await database.TopicsBySubjectAsync())["Conhecimentos locais"];
        local.Should().Equal("Curitiba (PR)", "Manaus (AM)", "Salvador (BA)", "Recife (PE)", "Goiânia (GO)", "Maceió (AL)");
        (await database.ScalarAsync(
            """
            SELECT count(*) FROM catalog.subjects WHERE normalized_name = 'HISTORIA E GEOGRAFIA DE CURITIBA'
            """)).Should().Be(0);
        (await database.ScalarAsync(
            """
            SELECT count(*) FROM catalog.topics WHERE normalized_name IN
              ('POVOS ORIGINARIOS', 'RELEVO, CLIMA, HIDROGRAFIA', 'URBANIZACAO',
               'PATRIMONIO HISTORICO (MATERIAL E IMATERIAL)', 'LEGISLACAO MUNICIPAL DE RECIFE', 'LEGISLACAO DE MACEIO')
            """)).Should().Be(0);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EverySeededRowEqualsWhatTheDomainComputes()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();
        var context = database.Context;

        var subjects = await context.Set<Subject>().AsNoTracking().ToListAsync();
        subjects.Should().HaveCount(23);
        foreach (var stored in subjects)
        {
            var rebuilt = Subject.Create(stored.Name, stored.AreaId);

            rebuilt.IsSuccess.Should().BeTrue(stored.Name);
            rebuilt.Value.NormalizedName.Should().Be(stored.NormalizedName, stored.Name);
            rebuilt.Value.Name.Should().Be(stored.Name);
            stored.NormalizedName.Should().Be(CatalogText.Normalize(stored.Name));
            stored.AreaId.Should().NotBeNull(stored.Name);
        }

        var topics = await context.Set<Topic>().AsNoTracking().ToListAsync();
        topics.Should().HaveCount(70);
        foreach (var stored in topics)
        {
            var rebuilt = Topic.Create(stored.SubjectId, stored.Name);

            rebuilt.IsSuccess.Should().BeTrue(stored.Name);
            rebuilt.Value.NormalizedName.Should().Be(stored.NormalizedName, stored.Name);
            rebuilt.Value.Name.Should().Be(stored.Name);
            stored.NormalizedName.Should().Be(CatalogText.Normalize(stored.Name));
        }
    }

    [Fact]
    public async Task Migrate_AnExistingSubjectWithTheSeededName_IsReusedAndKeepsItsNameAndNoArea()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(PreviousMigration);
        var existing = await database.InsertSubjectAsync("direito constitucional");
        await database.InsertTopicAsync(existing, "Crase");

        await database.MigrateAsync();

        var rows = await database.QueryAsync("SELECT name, area_id::text FROM catalog.subjects WHERE normalized_name = 'DIREITO CONSTITUCIONAL'");
        rows.Should().ContainSingle().Which.Should().Equal("direito constitucional", string.Empty);
        (await database.TopicsBySubjectAsync())["direito constitucional"].Should().BeEquivalentTo(
            "Crase", "Direitos fundamentais", "Constituição Federal (art. 144)", "Legislação de proteção", "Princípios constitucionais");
        (await database.ScalarAsync("SELECT count(*) FROM catalog.subjects")).Should().Be(23);
    }

    [Fact]
    public async Task Migrate_ASoftDeletedSubjectWithTheSeededName_IsNotInsertedAndItsTopicsAreSkipped()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(PreviousMigration);
        await database.InsertSubjectAsync("Geometria", deleted: true);

        await database.MigrateAsync();

        (await database.ScalarAsync("SELECT count(*) FROM catalog.subjects WHERE normalized_name = 'GEOMETRIA'")).Should().Be(1);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.subjects WHERE normalized_name = 'GEOMETRIA' AND is_deleted")).Should().Be(1);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.topics WHERE normalized_name = 'GEOMETRIA'")).Should().Be(0);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.topics")).Should().Be(69);
    }

    [Fact]
    public async Task Migrate_ATopicWithTheSeededNameInTheSubject_IsNotRepeated()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync(PreviousMigration);
        var existing = await database.InsertSubjectAsync("Informática");
        await database.InsertTopicAsync(existing, "LibreOffice");

        await database.MigrateAsync();

        var topics = (await database.TopicsBySubjectAsync())["Informática"];
        topics.Should().HaveCount(5);
        topics.Count(topic => topic == "LibreOffice").Should().Be(1);
    }

    [Fact]
    public async Task Migrate_AgainAfterAnAdminDeletedASeededTopic_KeepsItDeletedAndAddsNoDuplicate()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();
        await database.ExecuteAsync("UPDATE catalog.topics SET is_deleted = true, deleted_at = now() WHERE normalized_name = 'PONTUACAO'");

        await database.MigrateAsync();

        (await database.ScalarAsync("SELECT count(*) FROM catalog.topics")).Should().Be(70);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.topics WHERE normalized_name = 'PONTUACAO' AND is_deleted")).Should().Be(1);
    }

    [Fact]
    public async Task Down_RemovesTheSeededRowsAndKeepsASubjectWithTheAdminsOwnTopic()
    {
        await using var database = await DatabaseAsync();
        await database.MigrateAsync();
        var geometry = await database.ScalarGuidAsync("SELECT id FROM catalog.subjects WHERE normalized_name = 'GEOMETRIA'");
        await database.InsertTopicAsync(geometry, "Trigonometria");

        await database.MigrateAsync(PreviousMigration);

        (await database.ScalarAsync("SELECT count(*) FROM catalog.subjects")).Should().Be(1);
        (await database.ScalarAsync("SELECT count(*) FROM catalog.topics")).Should().Be(1);
        (await database.TopicsBySubjectAsync()).Should().ContainKey("Geometria").WhoseValue.Should().Equal("Trigonometria");
    }

    private static async Task<Database> DatabaseAsync()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(SeedGuardTaxonomyTests) + Guid.CreateVersion7().ToString("N"));
        var context = new CatalogModuleDbContext(
            new DbContextOptionsBuilder<CatalogModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());

        return new Database(context, connectionString);
    }

    private sealed class Database(CatalogModuleDbContext context, string connectionString) : IAsyncDisposable
    {
        public CatalogModuleDbContext Context { get; } = context;

        public Task MigrateAsync(string? target = null) => Context.GetService<IMigrator>().MigrateAsync(target);

        public async Task<Guid> InsertSubjectAsync(string name, bool deleted = false)
        {
            var id = Guid.CreateVersion7();
            await ExecuteAsync(
                "INSERT INTO catalog.subjects (id, name, normalized_name, tenant_id, created_at, is_deleted) VALUES (@id, @name, @normalized, NULL, now(), @deleted)",
                ("id", id), ("name", name), ("normalized", CatalogText.Normalize(name)), ("deleted", deleted));

            return id;
        }

        public Task InsertTopicAsync(Guid subjectId, string name) =>
            ExecuteAsync(
                "INSERT INTO catalog.topics (id, subject_id, name, normalized_name, tenant_id, created_at, is_deleted) VALUES (@id, @subject, @name, @normalized, NULL, now(), false)",
                ("id", Guid.CreateVersion7()), ("subject", subjectId), ("name", name), ("normalized", CatalogText.Normalize(name)));

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

        /// <summary>The live topics of each live subject, in the order they were created.</summary>
        public async Task<Dictionary<string, List<string>>> TopicsBySubjectAsync()
        {
            var rows = await QueryAsync(
                """
                SELECT s.name, t.name FROM catalog.subjects s
                LEFT JOIN catalog.topics t ON t.subject_id = s.id AND NOT t.is_deleted
                WHERE NOT s.is_deleted
                ORDER BY s.id, t.id
                """);
            var bySubject = new Dictionary<string, List<string>>();
            foreach (var row in rows)
            {
                if (!bySubject.TryGetValue(row[0], out var topics))
                {
                    topics = [];
                    bySubject[row[0]] = topics;
                }

                if (row[1].Length > 0)
                {
                    topics.Add(row[1]);
                }
            }

            return bySubject;
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
