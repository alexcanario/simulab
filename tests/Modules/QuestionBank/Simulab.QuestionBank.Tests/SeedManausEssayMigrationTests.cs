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
/// F-163 AC6 to AC8: the seed of the Manaus 2026 essay on databases that are not empty when it runs, and its rollback.
/// The catalog is migrated whole and the question bank up to the F-110 seed; the test changes the data, then applies the
/// essay seed. Each test gets its own database.
/// </summary>
public sealed class SeedManausEssayMigrationTests
{
    private const string GuardSeedMigration = "20261025190535_SeedManausGuardQuestions";
    private const string EssaySeedMigration = "SeedManausEssayQuestion";

    private static readonly Guid Manaus = SeedManausGuardQuestionsTests.Manaus;
    private static readonly Guid EssayId = SeedManausEssayTests.EssayId;
    private static readonly Guid EssayBaseTextId = SeedManausEssayTests.EssayBaseTextId;

    // AC6, BR4: a question already holding paper number 61, live or deleted, keeps the essay out.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Migrate_ThePaperNumberIsTaken_SeedsNothingAndSucceeds(bool deleted)
    {
        await using var database = await DatabaseAsync(withCatalog: true);
        var typed = await database.InsertQuestionAsync(Manaus, 61, deleted, Guid.CreateVersion7());

        await database.MigrateQuestionBankAsync();

        (await database.ValuesAsync($"SELECT id::text FROM question_bank.questions WHERE exam_edition_id = '{Manaus}' AND paper_number = 61"))
            .Should().Equal(typed.ToString());
        (await database.CountAsync($"SELECT count(*) FROM question_bank.base_texts WHERE id = '{EssayBaseTextId}'")).Should().Be(0);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.question_rubric_criteria WHERE question_id = '{EssayId}'")).Should().Be(0);
        (await database.AppliedAsync()).Should().Contain(name => name.EndsWith(EssaySeedMigration, StringComparison.Ordinal));
    }

    // AC6, BR4: the fixed id already in use, under another paper number, keeps the essay out as well.
    [Fact]
    public async Task Migrate_TheFixedIdIsTaken_SeedsNothingAndSucceeds()
    {
        await using var database = await DatabaseAsync(withCatalog: true);
        await database.InsertQuestionAsync(Manaus, 99, deleted: false, EssayId);

        await database.MigrateQuestionBankAsync();

        (await database.ValuesAsync($"SELECT paper_number::text FROM question_bank.questions WHERE id = '{EssayId}'")).Should().Equal("99");
        (await database.CountAsync($"SELECT count(*) FROM question_bank.base_texts WHERE id = '{EssayBaseTextId}'")).Should().Be(0);
        (await database.AppliedAsync()).Should().Contain(name => name.EndsWith(EssaySeedMigration, StringComparison.Ordinal));
    }

    // AC6, BR4: a deleted edition is skipped whole.
    [Fact]
    public async Task Migrate_TheEditionIsDeleted_SeedsNothingAndSucceeds()
    {
        await EssayIsSeededWithoutTheConditionAsync();
        await using var database = await DatabaseAsync(withCatalog: true);
        await database.ExecuteAsync($"UPDATE catalog.exam_editions SET is_deleted = true, deleted_at = now() WHERE id = '{Manaus}'");

        await database.MigrateQuestionBankAsync();

        (await database.CountAsync("SELECT count(*) FROM question_bank.questions WHERE type = 'Essay'")).Should().Be(0);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.base_texts WHERE id = '{EssayBaseTextId}'")).Should().Be(0);
        (await database.AppliedAsync()).Should().Contain(name => name.EndsWith(EssaySeedMigration, StringComparison.Ordinal));
    }

    // AC6, BR4: a database with no catalog schema (the question bank migrated alone) skips the seed.
    [Fact]
    public async Task Migrate_NoCatalogSchema_SeedsNothingAndSucceeds()
    {
        await EssayIsSeededWithoutTheConditionAsync();
        await using var database = await DatabaseAsync(withCatalog: false);

        await database.MigrateQuestionBankAsync();

        (await database.CountAsync("SELECT count(*) FROM question_bank.questions")).Should().Be(0);
        (await database.CountAsync("SELECT count(*) FROM question_bank.question_rubric_criteria")).Should().Be(0);
        (await database.AppliedAsync()).Should().Contain(name => name.EndsWith(EssaySeedMigration, StringComparison.Ordinal));
    }

    // AC7, BR4: the topic held under another id is found by its name.
    [Fact]
    public async Task Migrate_TheTopicIsUnderAnotherId_TheEssayPointsAtIt()
    {
        await using var database = await DatabaseAsync(withCatalog: true);
        var other = Guid.CreateVersion7();
        await database.ExecuteAsync($"UPDATE catalog.topics SET id = '{other}' WHERE id = '{SeedManausGuardQuestionsTests.Id(7, 2)}'");

        await database.MigrateQuestionBankAsync();

        (await database.ValuesAsync($"SELECT topic_id::text FROM question_bank.questions WHERE id = '{EssayId}'")).Should().Equal(other.ToString());
    }

    // AC7, BR4: a deleted topic skips the essay; the migration succeeds.
    [Fact]
    public async Task Migrate_TheTopicIsDeleted_SkipsTheEssayAndSucceeds()
    {
        await using var database = await DatabaseAsync(withCatalog: true);
        await database.ExecuteAsync($"UPDATE catalog.topics SET is_deleted = true, deleted_at = now() WHERE id = '{SeedManausGuardQuestionsTests.Id(7, 2)}'");

        await database.MigrateQuestionBankAsync();

        (await database.CountAsync("SELECT count(*) FROM question_bank.questions WHERE type = 'Essay'")).Should().Be(0);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.questions WHERE exam_edition_id = '{Manaus}'"))
            .Should().Be(SeedManausGuardQuestionsTests.Expected.Length);
        (await database.AppliedAsync()).Should().Contain(name => name.EndsWith(EssaySeedMigration, StringComparison.Ordinal));
    }

    // AC8, UC3, BR5: an untouched essay and its rows leave with the rollback; applying again writes it back once.
    [Fact]
    public async Task Rollback_AnUntouchedEssay_LeavesWithItsRows_AndMigratingAgainWritesItOnce()
    {
        await using var database = await DatabaseAsync(withCatalog: true);
        await database.MigrateQuestionBankAsync();
        (await database.CountAsync($"SELECT count(*) FROM question_bank.questions WHERE id = '{EssayId}'")).Should().Be(1);

        await database.MigrateQuestionBankAsync(GuardSeedMigration);

        (await database.CountAsync($"SELECT count(*) FROM question_bank.questions WHERE id = '{EssayId}'")).Should().Be(0);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.question_rubric_criteria WHERE question_id = '{EssayId}'")).Should().Be(0);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.question_versions WHERE question_id = '{EssayId}'")).Should().Be(0);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.base_texts WHERE id = '{EssayBaseTextId}'")).Should().Be(0);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.questions WHERE exam_edition_id = '{Manaus}'"))
            .Should().Be(SeedManausGuardQuestionsTests.Expected.Length);

        await database.MigrateQuestionBankAsync();

        (await database.CountAsync($"SELECT count(*) FROM question_bank.questions WHERE type = 'Essay'")).Should().Be(1);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.question_rubric_criteria WHERE question_id = '{EssayId}'")).Should().Be(6);
    }

    // AC8, UC3, BR5: an essay a curator edited stays when the seed is rolled back, with its rubric and base text.
    [Fact]
    public async Task Rollback_AnEditedEssay_Stays()
    {
        await using var database = await DatabaseAsync(withCatalog: true);
        await database.MigrateQuestionBankAsync();
        await database.ExecuteAsync($"UPDATE question_bank.questions SET statement_html = '<p>Revisado.</p>', updated_at = now() WHERE id = '{EssayId}'");

        await database.MigrateQuestionBankAsync(GuardSeedMigration);

        (await database.ValuesAsync($"SELECT statement_html FROM question_bank.questions WHERE id = '{EssayId}'")).Should().Equal("<p>Revisado.</p>");
        (await database.CountAsync($"SELECT count(*) FROM question_bank.question_rubric_criteria WHERE question_id = '{EssayId}'")).Should().Be(6);
        (await database.CountAsync($"SELECT count(*) FROM question_bank.base_texts WHERE id = '{EssayBaseTextId}'")).Should().Be(1);
        (await database.AppliedAsync()).Should().NotContain(name => name.EndsWith(EssaySeedMigration, StringComparison.Ordinal));

        await database.MigrateQuestionBankAsync();

        (await database.CountAsync("SELECT count(*) FROM question_bank.questions WHERE type = 'Essay'")).Should().Be(1);
        (await database.ValuesAsync($"SELECT statement_html FROM question_bank.questions WHERE id = '{EssayId}'")).Should().Equal("<p>Revisado.</p>");
    }

    // A test whose subject is "nothing was seeded" first proves the seed runs: the same steps on a database without the
    // condition seed the essay.
    private static async Task EssayIsSeededWithoutTheConditionAsync()
    {
        await using var control = await DatabaseAsync(withCatalog: true);
        await control.MigrateQuestionBankAsync();

        (await control.CountAsync("SELECT count(*) FROM question_bank.questions WHERE type = 'Essay'"))
            .Should().Be(1, "the control database is seeded");
    }

    private static async Task<Database> DatabaseAsync(bool withCatalog)
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(SeedManausEssayMigrationTests) + Guid.CreateVersion7().ToString("N"));
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

        var context = SeedManausGuardQuestionsMigrationTests.QuestionBankContext(connectionString);
        await context.GetService<IMigrator>().MigrateAsync(GuardSeedMigration);
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        return new Database(context, connection);
    }

    private sealed class Database(QuestionBankModuleDbContext context, NpgsqlConnection connection) : IAsyncDisposable
    {
        public Task MigrateQuestionBankAsync(string? target = null) => context.GetService<IMigrator>().MigrateAsync(target);

        public async Task<List<string>> AppliedAsync() => [.. await context.Database.GetAppliedMigrationsAsync()];

        public async Task<Guid> InsertQuestionAsync(Guid edition, int number, bool deleted, Guid id)
        {
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
