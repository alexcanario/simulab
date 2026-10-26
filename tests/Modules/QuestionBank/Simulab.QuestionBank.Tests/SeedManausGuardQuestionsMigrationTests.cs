using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.QuestionBank.Infrastructure.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-110 AC6 and AC7: the seed of the Manaus 2026 questions on a database that is not empty when it runs. The catalog is
/// migrated whole and the question bank up to the migration before the seed; the test changes the data, then applies the
/// seed. Each test gets its own database.
/// </summary>
public sealed class SeedManausGuardQuestionsMigrationTests
{
    internal const string PreviousMigration = "20261024214303_AddOpenAnswerAndEssayQuestions";
    private const string SeedMigration = "SeedManausGuardQuestions";

    private static readonly Guid Manaus = SeedManausGuardQuestionsTests.Manaus;

    // AC6, BR3: a question a curator typed into the edition before the seed, live or deleted, keeps the seed out.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Migrate_TheEditionAlreadyHoldsAQuestion_SeedsNothingAndSucceeds(bool deleted)
    {
        await SeedRunsWithoutTheConditionAsync();
        await using var database = await DatabaseAsync(withCatalog: true);
        var typed = await database.InsertQuestionAsync(Manaus, 1, deleted);

        await database.MigrateQuestionBankAsync();

        (await database.ValuesAsync($"SELECT id::text FROM question_bank.questions WHERE exam_edition_id = '{Manaus}'"))
            .Should().Equal(typed.ToString());
        (await database.CountAsync("SELECT count(*) FROM question_bank.base_texts")).Should().Be(0);
        (await database.CountAsync("SELECT count(*) FROM question_bank.question_images")).Should().Be(0);
        (await database.AppliedAsync()).Should().Contain(name => name.EndsWith(SeedMigration, StringComparison.Ordinal));
    }

    // AC6, BR3: a deleted edition is skipped whole.
    [Fact]
    public async Task Migrate_TheEditionIsDeleted_SeedsNothingAndSucceeds()
    {
        await SeedRunsWithoutTheConditionAsync();
        await using var database = await DatabaseAsync(withCatalog: true);
        await database.ExecuteAsync($"UPDATE catalog.exam_editions SET is_deleted = true, deleted_at = now() WHERE id = '{Manaus}'");

        await database.MigrateQuestionBankAsync();

        (await database.CountAsync("SELECT count(*) FROM question_bank.questions")).Should().Be(0);
        (await database.CountAsync("SELECT count(*) FROM question_bank.question_images")).Should().Be(0);
        (await database.AppliedAsync()).Should().Contain(name => name.EndsWith(SeedMigration, StringComparison.Ordinal));
    }

    // AC6, change note v2: a database with no catalog schema (the question bank migrated alone) skips the seed.
    [Fact]
    public async Task Migrate_NoCatalogSchema_SeedsNothingAndSucceeds()
    {
        await SeedRunsWithoutTheConditionAsync();
        await using var database = await DatabaseAsync(withCatalog: false);

        await database.MigrateQuestionBankAsync();

        (await database.CountAsync("SELECT count(*) FROM question_bank.questions")).Should().Be(0);
        (await database.CountAsync("SELECT count(*) FROM question_bank.base_texts")).Should().Be(0);
        (await database.AppliedAsync()).Should().Contain(name => name.EndsWith(SeedMigration, StringComparison.Ordinal));
    }

    // AC7, BR4: a topic of the classification that this database holds under another id is found by its name.
    [Fact]
    public async Task Migrate_ATopicUnderAnotherId_TheQuestionsOfThatTopicPointAtIt()
    {
        await using var database = await DatabaseAsync(withCatalog: true);
        var other = Guid.CreateVersion7();
        await database.ExecuteAsync($"UPDATE catalog.topics SET id = '{other}' WHERE id = '{SeedManausGuardQuestionsTests.Id(7, 1)}'");

        await database.MigrateQuestionBankAsync();

        var reading = SeedManausGuardQuestionsTests.Expected.Where(row => row.Topic == 1).Select(row => row.Number).ToList();
        reading.Should().NotBeEmpty();
        foreach (var number in reading)
        {
            (await database.ValuesAsync($"SELECT topic_id::text FROM question_bank.questions WHERE paper_number = {number} AND exam_edition_id = '{Manaus}'"))
                .Should().Equal([other.ToString()], $"question {number}");
        }

        (await database.CountAsync($"SELECT count(*) FROM question_bank.questions WHERE exam_edition_id = '{Manaus}'"))
            .Should().Be(SeedManausGuardQuestionsTests.Expected.Length);
    }

    // AC7, BR4: the questions of a deleted topic are skipped; every other question is seeded.
    [Fact]
    public async Task Migrate_ADeletedTopic_SkipsItsQuestionsAndSeedsTheOthers()
    {
        await using var database = await DatabaseAsync(withCatalog: true);
        await database.ExecuteAsync($"UPDATE catalog.topics SET is_deleted = true, deleted_at = now() WHERE id = '{SeedManausGuardQuestionsTests.Id(7, 6)}'");

        await database.MigrateQuestionBankAsync();

        var skipped = SeedManausGuardQuestionsTests.Expected.Where(row => row.Topic == 6).Select(row => row.Number).ToList();
        skipped.Should().Equal([7]);
        var seeded = (await database.ValuesAsync($"SELECT paper_number::text FROM question_bank.questions WHERE exam_edition_id = '{Manaus}' ORDER BY question_bank.questions.paper_number"))
            .Select(int.Parse).ToList();
        seeded.Should().Equal(SeedManausGuardQuestionsTests.Expected.Select(row => row.Number).Except(skipped));
        (await database.CountAsync($"SELECT count(*) FROM question_bank.question_options WHERE question_id = '{SeedManausGuardQuestionsTests.QuestionId(7)}'"))
            .Should().Be(0);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.question_versions WHERE question_id = '{SeedManausGuardQuestionsTests.QuestionId(7)}'"))
            .Should().Be(0);
    }

    // A test whose subject is "nothing was seeded" first proves the seed runs: the same steps on a database without the
    // condition seed the whole paper.
    private static async Task SeedRunsWithoutTheConditionAsync()
    {
        await using var control = await DatabaseAsync(withCatalog: true);
        await control.MigrateQuestionBankAsync();

        (await control.CountAsync($"SELECT count(*) FROM question_bank.questions WHERE exam_edition_id = '{Manaus}'"))
            .Should().Be(SeedManausGuardQuestionsTests.Expected.Length, "the control database is seeded");
    }

    private static async Task<Database> DatabaseAsync(bool withCatalog)
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(SeedManausGuardQuestionsMigrationTests) + Guid.CreateVersion7().ToString("N"));
        if (withCatalog)
        {
            await using var catalog = new CatalogModuleDbContext(
                new DbContextOptionsBuilder<CatalogModuleDbContext>()
                    .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(CatalogModuleDbContext.SchemaName))
                    .UseSnakeCaseNamingConvention()
                    .Options,
                new NoTenant());
            await catalog.GetService<IMigrator>().MigrateAsync();
        }

        var context = QuestionBankContext(connectionString);
        await context.GetService<IMigrator>().MigrateAsync(PreviousMigration);
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        return new Database(context, connection);
    }

    internal static QuestionBankModuleDbContext QuestionBankContext(string connectionString) =>
        new(
            new DbContextOptionsBuilder<QuestionBankModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(QuestionBankModuleDbContext.SchemaName))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());

    private sealed class Database(QuestionBankModuleDbContext context, NpgsqlConnection connection) : IAsyncDisposable
    {
        public Task MigrateQuestionBankAsync() => context.GetService<IMigrator>().MigrateAsync();

        public async Task<List<string>> AppliedAsync() => [.. await context.Database.GetAppliedMigrationsAsync()];

        public async Task<Guid> InsertQuestionAsync(Guid edition, int number, bool deleted)
        {
            var id = Guid.CreateVersion7();
            await ExecuteAsync(
                $"""
                INSERT INTO question_bank.questions (id, type, status, topic_id, content_language, statement_html, statement_text,
                    normalized_statement, exam_edition_id, paper_number, created_at, is_deleted)
                VALUES ('{id}', 'SingleChoice', 'Draft', '{Guid.CreateVersion7()}', 'pt-BR', '<p>x</p>', 'x', 'X', '{edition}', {number}, now(), {(deleted ? "true" : "false")})
                """);

            return id;
        }

        public async Task<int> ExecuteAsync(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection);

            return await command.ExecuteNonQueryAsync();
        }

        public async Task<long> CountAsync(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection);

            return (long)(await command.ExecuteScalarAsync())!;
        }

        public async Task<List<string>> ValuesAsync(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var values = new List<string>();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(0));
            }

            return values;
        }

        public async ValueTask DisposeAsync()
        {
            await connection.DisposeAsync();
            await context.DisposeAsync();
        }
    }
}
