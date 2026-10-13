using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.Persistence;
using Simulab.QuestionBank.Infrastructure.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-101 AC12: applied to a database that has the F-98 migration with questions and versions in it, the new
/// migration changes no stored status, gives <c>annulled</c> false to every version, and the database refuses an
/// annulled version with no reason and one that is also a key change; the Down turns annulled questions back
/// into drafts so the previous binary still reads every row (design trade-off 9).
/// </summary>
public sealed class AddQuestionAnnulmentMigrationTests
{
    private const string PreviousMigration = "20261012221419_AddQuestionVersions";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_KeepsEveryStoredStatus_AndGivesAnnulledFalseToEveryStoredVersion()
    {
        await using var database = await MigratedAsync(nameof(Up_KeepsEveryStoredStatus_AndGivesAnnulledFalseToEveryStoredVersion));
        var published = await InsertQuestionAsync(database.Connection, "Published");
        var draft = await InsertQuestionAsync(database.Connection, "Draft");
        await InsertVersionAsync(database.Connection, published, 1, "<p>Qual?</p>");

        await database.Migrator.MigrateAsync();

        (await ValuesAsync(database.Connection, $"SELECT id::text || ':' || status FROM {Schema}.questions ORDER BY status"))
            .Should().BeEquivalentTo([$"{draft}:Draft", $"{published}:Published"]);
        (await ValuesAsync(database.Connection, $"SELECT annulled::text FROM {Schema}.question_versions WHERE question_id = '{published}'"))
            .Should().Equal("false");
        (await ValuesAsync(database.Connection, $"SELECT is_nullable || ':' || coalesce(column_default, 'none') FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'question_versions' AND column_name = 'annulled'"))
            .Should().Equal("NO:false");
    }

    [Fact]
    public async Task TheDatabase_RefusesAnAnnulledVersionWithNoReason_AndOneThatIsAlsoAKeyChange()
    {
        await using var database = await MigratedAsync(nameof(TheDatabase_RefusesAnAnnulledVersionWithNoReason_AndOneThatIsAlsoAKeyChange));
        await database.Migrator.MigrateAsync();
        var question = await InsertQuestionAsync(database.Connection, "Published");

        await InsertAnnulledVersionAsync(database.Connection, question, 1, annulled: true, answerKeyChanged: false, reason: "Anulada.");
        await InsertAnnulledVersionAsync(database.Connection, question, 2, annulled: false, answerKeyChanged: false, reason: null);

        foreach (var (number, annulled, changed, reason) in new (int, bool, bool, string?)[]
        {
            (3, true, false, null),
            (4, true, true, "Troca e anulação.")
        })
        {
            var refused = async () => await InsertAnnulledVersionAsync(database.Connection, question, number, annulled, changed, reason);

            (await refused.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
        }

        (await ValuesAsync(database.Connection, $"SELECT conname FROM pg_constraint WHERE conrelid = '{Schema}.question_versions'::regclass AND contype = 'c' ORDER BY conname"))
            .Should().Contain("ck_question_versions_annulled_reason")
            .And.Contain("ck_question_versions_annulled_key_changed");
    }

    [Fact]
    public async Task Down_TurnsAnnulledQuestionsBackIntoDrafts_AndDropsTheColumnAndTheChecks()
    {
        await using var database = await MigratedAsync(nameof(Down_TurnsAnnulledQuestionsBackIntoDrafts_AndDropsTheColumnAndTheChecks));
        await database.Migrator.MigrateAsync();
        var annulled = await InsertQuestionAsync(database.Connection, "Annulled");
        var published = await InsertQuestionAsync(database.Connection, "Published");
        await InsertAnnulledVersionAsync(database.Connection, annulled, 1, annulled: true, answerKeyChanged: false, reason: "Anulada.");

        await database.Migrator.MigrateAsync(PreviousMigration);

        (await ValuesAsync(database.Connection, $"SELECT id::text || ':' || status FROM {Schema}.questions ORDER BY status"))
            .Should().BeEquivalentTo([$"{annulled}:Draft", $"{published}:Published"]);
        (await ValuesAsync(database.Connection, $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'question_versions' AND column_name = 'annulled'"))
            .Should().BeEmpty();
        (await ValuesAsync(database.Connection, $"SELECT conname FROM pg_constraint WHERE conrelid = '{Schema}.question_versions'::regclass AND conname LIKE 'ck_question_versions_annulled%'"))
            .Should().BeEmpty();
        (await ValuesAsync(database.Connection, $"SELECT question_id::text FROM {Schema}.question_versions"))
            .Should().Equal(annulled.ToString());
    }

    private static async Task<Database> MigratedAsync(string name)
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(name);
        var context = new QuestionBankModuleDbContext(
            new DbContextOptionsBuilder<QuestionBankModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        return new Database(context, connection, migrator);
    }

    private static async Task<Guid> InsertQuestionAsync(NpgsqlConnection connection, string status)
    {
        var id = Guid.CreateVersion7();
        await using var command = new NpgsqlCommand(
            $@"INSERT INTO {Schema}.questions
                (id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement, created_at, is_deleted)
               VALUES (@id, 'SingleChoice', @status, @topic, 'pt-BR', '<p>Qual?</p>', 'x', 'x', now(), false)",
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("topic", Guid.CreateVersion7());
        await command.ExecuteNonQueryAsync();

        return id;
    }

    // Before the migration: the column does not exist yet, so only the F-98 columns are written.
    private static async Task InsertVersionAsync(NpgsqlConnection connection, Guid questionId, int number, string statement)
    {
        await using var command = new NpgsqlCommand(
            $@"INSERT INTO {Schema}.question_versions
                (id, question_id, number, type, content_language, statement_html, options, answer_key_changed, created_at, is_deleted)
               VALUES (@id, @question, @number, 'SingleChoice', 'pt-BR', @statement, '[]'::jsonb, false, now(), false)",
            connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("question", questionId);
        command.Parameters.AddWithValue("number", number);
        command.Parameters.AddWithValue("statement", statement);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertAnnulledVersionAsync(
        NpgsqlConnection connection,
        Guid questionId,
        int number,
        bool annulled,
        bool answerKeyChanged,
        string? reason)
    {
        await using var command = new NpgsqlCommand(
            $@"INSERT INTO {Schema}.question_versions
                (id, question_id, number, type, content_language, statement_html, options, answer_key_changed, annulled, reason, created_at, is_deleted)
               VALUES (@id, @question, @number, 'SingleChoice', 'pt-BR', '<p>x</p>', '[]'::jsonb, @changed, @annulled, @reason, now(), false)",
            connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("question", questionId);
        command.Parameters.AddWithValue("number", number);
        command.Parameters.AddWithValue("changed", answerKeyChanged);
        command.Parameters.AddWithValue("annulled", annulled);
        command.Parameters.AddWithValue("reason", (object?)reason ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> ValuesAsync(NpgsqlConnection connection, string sql)
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

    private sealed class Database(QuestionBankModuleDbContext context, NpgsqlConnection connection, IMigrator migrator)
        : IAsyncDisposable
    {
        public NpgsqlConnection Connection { get; } = connection;

        public IMigrator Migrator { get; } = migrator;

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
            await context.DisposeAsync();
        }
    }
}
