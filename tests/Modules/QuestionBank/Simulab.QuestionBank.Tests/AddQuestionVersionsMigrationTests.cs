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
/// F-98 AC13, BR9: applied to a database that has the F-97 migration and questions in it, the new migration
/// creates the versions table and gives version 1 to every published question that was not deleted, with the
/// stored content and the options that were live, and to no draft; and the database refuses a key change with no
/// reason.
/// </summary>
public sealed class AddQuestionVersionsMigrationTests
{
    private const string PreviousMigration = "20261012200232_AddTrueFalseKey";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_GivesVersionOneToEachPublishedQuestionNotDeleted_AndToNoDraft()
    {
        await using var database = await MigratedAsync(nameof(Up_GivesVersionOneToEachPublishedQuestionNotDeleted_AndToNoDraft));
        var singleChoice = await InsertQuestionAsync(database.Connection, "SingleChoice", "Published", false, null, "<p>Qual?</p>", "<p>Porque sim.</p>");
        await InsertOptionAsync(database.Connection, singleChoice, 0, "<p>Paris</p>", true, false);
        await InsertOptionAsync(database.Connection, singleChoice, 1, "<p>Lyon</p>", false, false);
        await InsertOptionAsync(database.Connection, singleChoice, 2, "<p>Orfã</p>", false, true);
        var trueFalse = await InsertQuestionAsync(database.Connection, "TrueFalse", "Published", false, true, "<p>Paris é a capital.</p>", null);
        var draft = await InsertQuestionAsync(database.Connection, "SingleChoice", "Draft", false, null, "<p>Rascunho.</p>", null);
        var deleted = await InsertQuestionAsync(database.Connection, "SingleChoice", "Published", true, null, "<p>Excluída.</p>", null);

        await database.Migrator.MigrateAsync();

        (await ValuesAsync(database.Connection, $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}' AND table_name = 'question_versions'"))
            .Should().ContainSingle();
        (await ValuesAsync(database.Connection, $"SELECT question_id::text FROM {Schema}.question_versions"))
            .Should().BeEquivalentTo([singleChoice.ToString(), trueFalse.ToString()]);
        (await ValuesAsync(database.Connection, $"SELECT number::text || ':' || type || ':' || statement_html || ':' || coalesce(explanation_html, '-') || ':' || coalesce(true_false_key::text, 'null') || ':' || answer_key_changed::text || ':' || coalesce(reason, 'null') || ':' || coalesce(created_by::text, 'null') FROM {Schema}.question_versions WHERE question_id = '{singleChoice}'"))
            .Should().Equal("1:SingleChoice:<p>Qual?</p>:<p>Porque sim.</p>:null:false:null:null");
        (await ValuesAsync(database.Connection, $"SELECT number::text || ':' || type || ':' || statement_html || ':' || coalesce(true_false_key::text, 'null') || ':' || jsonb_array_length(options)::text FROM {Schema}.question_versions WHERE question_id = '{trueFalse}'"))
            .Should().Equal("1:TrueFalse:<p>Paris é a capital.</p>:true:0");
        (await ValuesAsync(database.Connection, $"SELECT question_id::text FROM {Schema}.question_versions WHERE question_id IN ('{draft}', '{deleted}')"))
            .Should().BeEmpty();
    }

    // The options keep their position, text and mark, under the member names the mapping reads (Position,
    // TextHtml, IsCorrect), and a soft-deleted option row of a live question is not part of the snapshot.
    [Fact]
    public async Task Up_CopiesTheLiveOptionsInPositionOrder_UnderTheNamesTheMappingReads()
    {
        await using var database = await MigratedAsync(nameof(Up_CopiesTheLiveOptionsInPositionOrder_UnderTheNamesTheMappingReads));
        var question = await InsertQuestionAsync(database.Connection, "SingleChoice", "Published", false, null, "<p>Qual?</p>", null);
        await InsertOptionAsync(database.Connection, question, 1, "<p>Lyon</p>", false, false);
        await InsertOptionAsync(database.Connection, question, 0, "<p>Paris</p>", true, false);
        await InsertOptionAsync(database.Connection, question, 2, "<p>Orfã</p>", false, true);

        await database.Migrator.MigrateAsync();

        (await ValuesAsync(database.Connection, $"SELECT (e->>'Position') || ':' || (e->>'TextHtml') || ':' || (e->>'IsCorrect') FROM {Schema}.question_versions v, jsonb_array_elements(v.options) WITH ORDINALITY AS t(e, n) WHERE v.question_id = '{question}' ORDER BY n"))
            .Should().Equal("0:<p>Paris</p>:true", "1:<p>Lyon</p>:false");
    }

    [Fact]
    public async Task Up_Then_Down_RemovesTheTable_AndTheQuestionsAreUntouched()
    {
        await using var database = await MigratedAsync(nameof(Up_Then_Down_RemovesTheTable_AndTheQuestionsAreUntouched));
        var question = await InsertQuestionAsync(database.Connection, "SingleChoice", "Published", false, null, "<p>Qual?</p>", null);
        await database.Migrator.MigrateAsync();
        (await ValuesAsync(database.Connection, $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}' AND table_name = 'question_versions'"))
            .Should().ContainSingle();

        await database.Migrator.MigrateAsync(PreviousMigration);

        (await ValuesAsync(database.Connection, $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}' AND table_name = 'question_versions'"))
            .Should().BeEmpty();
        (await ValuesAsync(database.Connection, $"SELECT id::text FROM {Schema}.questions"))
            .Should().Equal(question.ToString());
    }

    // BR5 and BR7 in the database itself: a key change with no reason, and a number below 1, are refused; the
    // unique index and its tenant column are named as the profile asks.
    [Fact]
    public async Task TheDatabase_RefusesAKeyChangeWithNoReason_AndANumberBelowOne_AndNamesItsIndexes()
    {
        await using var database = await MigratedAsync(nameof(TheDatabase_RefusesAKeyChangeWithNoReason_AndANumberBelowOne_AndNamesItsIndexes));
        await database.Migrator.MigrateAsync();
        var question = await InsertQuestionAsync(database.Connection, "SingleChoice", "Published", false, null, "<p>Qual?</p>", null);

        await InsertVersionAsync(database.Connection, question, 1, false, null);
        await InsertVersionAsync(database.Connection, question, 2, true, "Recurso deferido.");

        foreach (var (number, changed, reason) in new (int, bool, string?)[] { (3, true, null), (0, false, null) })
        {
            var refused = async () => await InsertVersionAsync(database.Connection, question, number, changed, reason);

            (await refused.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
        }

        var duplicate = async () => await InsertVersionAsync(database.Connection, question, 2, false, null);
        (await duplicate.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23505");
        (await ValuesAsync(database.Connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'question_versions'"))
            .Should().Contain("ux_question_versions_tenant_question_number")
            .And.Contain("ix_question_versions_question");
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

    private static async Task<Guid> InsertQuestionAsync(
        NpgsqlConnection connection,
        string type,
        string status,
        bool isDeleted,
        bool? key,
        string statementHtml,
        string? explanationHtml)
    {
        var id = Guid.CreateVersion7();
        await using var command = new NpgsqlCommand(
            $@"INSERT INTO {Schema}.questions
                (id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement,
                 explanation_html, true_false_key, created_at, is_deleted)
               VALUES (@id, @type, @status, @topic, 'pt-BR', @statement, 'x', 'x', @explanation, @key, now(), @deleted)",
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("type", type);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("topic", Guid.CreateVersion7());
        command.Parameters.AddWithValue("statement", statementHtml);
        command.Parameters.AddWithValue("explanation", (object?)explanationHtml ?? DBNull.Value);
        command.Parameters.AddWithValue("key", (object?)key ?? DBNull.Value);
        command.Parameters.AddWithValue("deleted", isDeleted);
        await command.ExecuteNonQueryAsync();

        return id;
    }

    private static async Task InsertOptionAsync(
        NpgsqlConnection connection,
        Guid questionId,
        int position,
        string textHtml,
        bool isCorrect,
        bool isDeleted)
    {
        await using var command = new NpgsqlCommand(
            $@"INSERT INTO {Schema}.question_options (id, question_id, position, text_html, is_correct, created_at, is_deleted)
               VALUES (@id, @question, @position, @text, @correct, now(), @deleted)",
            connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("question", questionId);
        command.Parameters.AddWithValue("position", position);
        command.Parameters.AddWithValue("text", textHtml);
        command.Parameters.AddWithValue("correct", isCorrect);
        command.Parameters.AddWithValue("deleted", isDeleted);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertVersionAsync(
        NpgsqlConnection connection,
        Guid questionId,
        int number,
        bool answerKeyChanged,
        string? reason)
    {
        await using var command = new NpgsqlCommand(
            $@"INSERT INTO {Schema}.question_versions
                (id, question_id, number, type, content_language, statement_html, options, answer_key_changed, reason, created_at, is_deleted)
               VALUES (@id, @question, @number, 'SingleChoice', 'pt-BR', '<p>x</p>', '[]'::jsonb, @changed, @reason, now(), false)",
            connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("question", questionId);
        command.Parameters.AddWithValue("number", number);
        command.Parameters.AddWithValue("changed", answerKeyChanged);
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
