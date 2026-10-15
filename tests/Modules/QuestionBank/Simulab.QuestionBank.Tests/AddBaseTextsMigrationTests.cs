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
/// F-102 AC7, BR10: applied to a database that has the F-101 migration and questions with inline base text in
/// it, the new migration gives each of those questions a base text of its own (soft-deleted when the question
/// was), keeps the language and the HTML, writes no version and drops the inline column; and the down migration
/// copies the text back. The database refuses a blank title and a blank text.
/// </summary>
public sealed class AddBaseTextsMigrationTests
{
    private const string PreviousMigration = "20261013211727_AddQuestionAnnulment";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_GivesEachQuestionWithInlineTextABaseTextOfItsOwn_AndLinksIt()
    {
        await using var database = await MigratedAsync(nameof(Up_GivesEachQuestionWithInlineTextABaseTextOfItsOwn_AndLinksIt));
        var live = await InsertQuestionAsync(database.Connection, "en", "<p>The river ran <strong>slowly</strong> &amp; quiet.</p>", false);
        var other = await InsertQuestionAsync(database.Connection, "pt-BR", "<p>Primeira linha.</p><p>Segunda linha.</p>", false);
        var none = await InsertQuestionAsync(database.Connection, "pt-BR", null, false);
        var blank = await InsertQuestionAsync(database.Connection, "pt-BR", "", false);

        await database.Migrator.MigrateAsync();

        (await ValuesAsync(database.Connection, $@"SELECT b.content_language || '|' || b.title || '|' || b.text_html || '|' || b.is_deleted::text
            FROM {Schema}.questions q JOIN {Schema}.base_texts b ON b.id = q.base_text_id WHERE q.id = '{live}'"))
            .Should().Equal("en|The river ran slowly & quiet.|<p>The river ran <strong>slowly</strong> &amp; quiet.</p>|false");
        (await ValuesAsync(database.Connection, $@"SELECT b.title FROM {Schema}.questions q
            JOIN {Schema}.base_texts b ON b.id = q.base_text_id WHERE q.id = '{other}'"))
            .Should().Equal("Primeira linha. Segunda linha.");
        (await ValuesAsync(database.Connection, $"SELECT coalesce(base_text_id::text, '-') FROM {Schema}.questions WHERE id IN ('{none}', '{blank}')"))
            .Should().Equal("-", "-");
        (await ValuesAsync(database.Connection, $"SELECT count(*)::text FROM {Schema}.base_texts")).Should().Equal("2");
    }

    // BR10: the title is the first 80 characters of the plain text.
    [Fact]
    public async Task Up_CutsTheTitleAtEightyCharacters()
    {
        await using var database = await MigratedAsync(nameof(Up_CutsTheTitleAtEightyCharacters));
        var long80 = new string('a', 100);
        await InsertQuestionAsync(database.Connection, "pt-BR", $"<p>{long80}</p>", false);

        await database.Migrator.MigrateAsync();

        (await ValuesAsync(database.Connection, $"SELECT length(title)::text FROM {Schema}.base_texts")).Should().Equal("80");
    }

    // AC7, BR10: a soft-deleted question's text is soft-deleted with its date; versions are untouched.
    [Fact]
    public async Task Up_SoftDeletesTheTextOfASoftDeletedQuestion_AndWritesNoVersion()
    {
        await using var database = await MigratedAsync(nameof(Up_SoftDeletesTheTextOfASoftDeletedQuestion_AndWritesNoVersion));
        var deleted = await InsertQuestionAsync(database.Connection, "pt-BR", "<p>Texto antigo.</p>", true);
        var published = await InsertQuestionAsync(database.Connection, "pt-BR", "<p>Texto vivo.</p>", false, "Published");
        await InsertVersionAsync(database.Connection, published, "<p>Texto vivo.</p>");
        var versionsBefore = await ValuesAsync(database.Connection, $"SELECT count(*)::text FROM {Schema}.question_versions");

        await database.Migrator.MigrateAsync();

        (await ValuesAsync(database.Connection, $@"SELECT (b.is_deleted AND b.deleted_at = q.deleted_at)::text FROM {Schema}.questions q
            JOIN {Schema}.base_texts b ON b.id = q.base_text_id WHERE q.id = '{deleted}'"))
            .Should().Equal("true");
        (await ValuesAsync(database.Connection, $"SELECT count(*)::text FROM {Schema}.question_versions")).Should().Equal(versionsBefore);
        (await ValuesAsync(database.Connection, $"SELECT base_text_html FROM {Schema}.question_versions"))
            .Should().Equal("<p>Texto vivo.</p>");
        (await ValuesAsync(database.Connection,
            $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'questions' AND column_name = 'base_text_html'"))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Up_Then_Down_CopiesTheTextBackIntoTheQuestions_AndDropsTheTable()
    {
        await using var database = await MigratedAsync(nameof(Up_Then_Down_CopiesTheTextBackIntoTheQuestions_AndDropsTheTable));
        var question = await InsertQuestionAsync(database.Connection, "pt-BR", "<p>Texto que volta.</p>", false);
        await database.Migrator.MigrateAsync();
        (await ValuesAsync(database.Connection, $"SELECT text_html FROM {Schema}.base_texts"))
            .Should().Equal("<p>Texto que volta.</p>");

        await database.Migrator.MigrateAsync(PreviousMigration);

        (await ValuesAsync(database.Connection, $"SELECT base_text_html FROM {Schema}.questions WHERE id = '{question}'"))
            .Should().Equal("<p>Texto que volta.</p>");
        (await ValuesAsync(database.Connection,
            $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}' AND table_name = 'base_texts'"))
            .Should().BeEmpty();
        (await ValuesAsync(database.Connection,
            $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'questions' AND column_name = 'base_text_id'"))
            .Should().BeEmpty();
    }

    // The database itself refuses what the entity refuses; the foreign key restricts a hard delete.
    [Fact]
    public async Task TheDatabase_RefusesABlankTitleAndABlankText_AndAHardDeleteOfALinkedText()
    {
        await using var database = await MigratedAsync(nameof(TheDatabase_RefusesABlankTitleAndABlankText_AndAHardDeleteOfALinkedText));
        await database.Migrator.MigrateAsync();
        var linked = await InsertBaseTextAsync(database.Connection, "Texto", "<p>x</p>");
        var question = await InsertQuestionAsync(database.Connection, "pt-BR", null, false);
        await using (var link = new NpgsqlCommand(
            $"UPDATE {Schema}.questions SET base_text_id = @text WHERE id = @question", database.Connection))
        {
            link.Parameters.AddWithValue("text", linked);
            link.Parameters.AddWithValue("question", question);
            await link.ExecuteNonQueryAsync();
        }

        foreach (var (title, html) in new[] { ("   ", "<p>x</p>"), ("Texto", "") })
        {
            var refused = async () => await InsertBaseTextAsync(database.Connection, title, html);

            (await refused.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
        }

        var hardDelete = async () =>
        {
            await using var command = new NpgsqlCommand($"DELETE FROM {Schema}.base_texts WHERE id = @id", database.Connection);
            command.Parameters.AddWithValue("id", linked);
            await command.ExecuteNonQueryAsync();
        };
        (await hardDelete.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23503");
        (await ValuesAsync(database.Connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'questions'"))
            .Should().Contain("ix_questions_base_text");
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
        string language,
        string? baseTextHtml,
        bool isDeleted,
        string status = "Draft")
    {
        var id = Guid.CreateVersion7();
        await using var command = new NpgsqlCommand(
            $@"INSERT INTO {Schema}.questions
                (id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement,
                 base_text_html, created_at, is_deleted, deleted_at)
               VALUES (@id, 'SingleChoice', @status, @topic, @language, '<p>Qual?</p>', 'Qual?', 'QUAL?', @base, now(), @deleted,
                       CASE WHEN @deleted THEN now() - interval '1 day' ELSE NULL END)",
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("topic", Guid.CreateVersion7());
        command.Parameters.AddWithValue("language", language);
        command.Parameters.AddWithValue("base", (object?)baseTextHtml ?? DBNull.Value);
        command.Parameters.AddWithValue("deleted", isDeleted);
        await command.ExecuteNonQueryAsync();

        return id;
    }

    private static async Task InsertVersionAsync(NpgsqlConnection connection, Guid questionId, string baseTextHtml)
    {
        await using var command = new NpgsqlCommand(
            $@"INSERT INTO {Schema}.question_versions
                (id, question_id, number, type, content_language, statement_html, base_text_html, options, answer_key_changed, created_at, is_deleted)
               VALUES (@id, @question, 1, 'SingleChoice', 'pt-BR', '<p>Qual?</p>', @base, '[]'::jsonb, false, now(), false)",
            connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("question", questionId);
        command.Parameters.AddWithValue("base", baseTextHtml);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<Guid> InsertBaseTextAsync(NpgsqlConnection connection, string title, string html)
    {
        var id = Guid.CreateVersion7();
        await using var command = new NpgsqlCommand(
            $@"INSERT INTO {Schema}.base_texts (id, title, normalized_title, content_language, text_html, created_at, is_deleted)
               VALUES (@id, @title, upper(@title), 'pt-BR', @html, now(), false)",
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("title", title);
        command.Parameters.AddWithValue("html", html);
        await command.ExecuteNonQueryAsync();

        return id;
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
