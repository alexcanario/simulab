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
/// F-97 AC13: applied to a database that has the F-96 migration and questions in it, the new migration adds the
/// nullable key column and its check in a real PostgreSQL, leaves every existing question with its type and no
/// key, and the database itself refuses a key on a row that is not a true/false question.
/// </summary>
public sealed class AddTrueFalseKeyMigrationTests
{
    private const string PreviousMigration = "20261012133630_AddQuestionOrigin";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_AddsTheKeyColumnAndItsCheck_AndKeepsEveryExistingQuestionWithItsTypeAndNoKey()
    {
        await using var database = await MigratedAsync(nameof(Up_AddsTheKeyColumnAndItsCheck_AndKeepsEveryExistingQuestionWithItsTypeAndNoKey));
        await InsertQuestionAsync(database.Connection, "SingleChoice", null);

        await database.Migrator.MigrateAsync();

        (await ValuesAsync(database.Connection, $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'questions'"))
            .Should().Contain("true_false_key")
            .And.NotContain(name => name.Contains("penalt") || name.Contains("weight") || name.Contains("score") || name.Contains("fraction"));
        (await ValuesAsync(database.Connection, "SELECT conname FROM pg_constraint WHERE contype = 'c' AND conname = 'ck_questions_true_false_key'"))
            .Should().ContainSingle();
        (await ValuesAsync(database.Connection, $"SELECT type || ':' || coalesce(true_false_key::text, 'null') FROM {Schema}.questions"))
            .Should().Equal("SingleChoice:null");

        await database.Migrator.MigrateAsync(PreviousMigration);

        (await ValuesAsync(database.Connection, $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'questions'"))
            .Should().NotContain("true_false_key");
    }

    [Fact]
    public async Task TheDatabase_RefusesAKeyOnASingleChoiceRow_ButAcceptsOneOnATrueFalseRowAndNoneAnywhere()
    {
        await using var database = await MigratedAsync(nameof(TheDatabase_RefusesAKeyOnASingleChoiceRow_ButAcceptsOneOnATrueFalseRowAndNoneAnywhere));
        await database.Migrator.MigrateAsync();

        await InsertQuestionAsync(database.Connection, "TrueFalse", true);
        await InsertQuestionAsync(database.Connection, "TrueFalse", false);
        await InsertQuestionAsync(database.Connection, "TrueFalse", null);
        await InsertQuestionAsync(database.Connection, "SingleChoice", null);

        foreach (var key in new[] { true, false })
        {
            var refused = async () => await InsertQuestionAsync(database.Connection, "SingleChoice", key);

            (await refused.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
        }
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

    private static async Task InsertQuestionAsync(NpgsqlConnection connection, string type, bool? key)
    {
        var columns = "id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement, created_at, is_deleted";
        var values = "@id, @type, 'Draft', @topic, 'pt-BR', '<p>x</p>', 'x', 'x', now(), false";
        var hasKey = await ColumnExistsAsync(connection, "true_false_key");
        if (hasKey)
        {
            columns += ", true_false_key";
            values += ", @key";
        }

        await using var command = new NpgsqlCommand($"INSERT INTO {Schema}.questions ({columns}) VALUES ({values})", connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("type", type);
        command.Parameters.AddWithValue("topic", Guid.CreateVersion7());
        if (hasKey)
        {
            command.Parameters.AddWithValue("key", (object?)key ?? DBNull.Value);
        }

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> ColumnExistsAsync(NpgsqlConnection connection, string column) =>
        (await ValuesAsync(connection, $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'questions' AND column_name = '{column}'")).Count > 0;

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
