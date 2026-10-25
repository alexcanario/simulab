using System.Globalization;
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
/// F-109 AC17: applied to a database that has a short-answer question with its blank and its version, the migration
/// adds the rubric table and the size columns, names the seventh and eighth type in the `type` comments, and every row
/// keeps its type, its blank and its version (a version recorded before it reads as an empty rubric and no size).
/// </summary>
public sealed class AddOpenAnswerAndEssayQuestionsMigrationTests
{
    private const string PreviousMigration = "20261024122942_AddShortAnswerQuestions";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_AddsTheRubricTableAndTheSizeColumns_NamesTheTypes_AndKeepsEveryRow()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(
            nameof(Up_AddsTheRubricTableAndTheSizeColumns_NamesTheTypes_AndKeepsEveryRow));
        await using var context = new QuestionBankModuleDbContext(
            new DbContextOptionsBuilder<QuestionBankModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var questionId = Guid.CreateVersion7();
        await ExecuteAsync(
            connection,
            $$"""
            INSERT INTO {{Schema}}.questions (id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement, created_at, is_deleted)
            VALUES ('{{questionId}}', 'ShortAnswer', 'Published', '{{Guid.CreateVersion7()}}', 'pt-BR', '<p>Capital?</p>', 'Capital?', 'CAPITAL?', now(), false);
            INSERT INTO {{Schema}}.question_blanks (id, question_id, number, position, accepted_answers, created_at, is_deleted)
            VALUES ('{{Guid.CreateVersion7()}}', '{{questionId}}', 1, 0, ARRAY['Brasília'], now(), false);
            INSERT INTO {{Schema}}.question_versions (id, question_id, number, type, content_language, statement_html, options, match_items, blanks, answer_key_changed, annulled, created_at, is_deleted)
            VALUES ('{{Guid.CreateVersion7()}}', '{{questionId}}', 1, 'ShortAnswer', 'pt-BR', '<p>Capital?</p>', '[]'::jsonb, '[]'::jsonb, '[{"Number":1,"AcceptedAnswers":["Brasília"]}]'::jsonb, false, false, now(), false);
            """);

        (await TableCountAsync(connection)).Should().Be(0, "the rubric table does not exist before the migration");

        await migrator.MigrateAsync();

        (await TableCountAsync(connection)).Should().Be(1);
        (await ValuesAsync(
                connection,
                $"""
                SELECT column_name FROM information_schema.columns
                WHERE table_schema = '{Schema}' AND table_name = 'question_rubric_criteria' ORDER BY column_name
                """))
            .Should().Contain(["description", "max_points", "name", "position", "question_id"]);
        (await ValuesAsync(
                connection,
                $"""
                SELECT table_name || '.' || column_name FROM information_schema.columns
                WHERE table_schema = '{Schema}' AND column_name IN ('answer_size_min', 'answer_size_max', 'rubric') ORDER BY 1
                """))
            .Should().Equal(
                "question_versions.answer_size_max",
                "question_versions.answer_size_min",
                "question_versions.rubric",
                "questions.answer_size_max",
                "questions.answer_size_min");
        var comments = await TypeCommentsAsync(connection);
        comments.Should().HaveCount(2);
        comments.Should().OnlyContain(comment => comment.Contains("ShortAnswer") && comment.Contains("OpenAnswer") && comment.Contains("Essay"));
        (await ValuesAsync(connection, $"SELECT type FROM {Schema}.questions")).Should().Equal("ShortAnswer");
        (await ValuesAsync(connection, $"SELECT accepted_answers[1] FROM {Schema}.question_blanks")).Should().Equal("Brasília");

        var version = await context.QuestionVersions.AsNoTracking().SingleAsync();
        version.Number.Should().Be(1);
        version.Blanks.Should().ContainSingle().Which.AcceptedAnswers.Should().Equal("Brasília");
        version.Rubric.Should().BeEmpty();
        version.AnswerSizeMin.Should().BeNull();
        version.AnswerSizeMax.Should().BeNull();
    }

    [Fact]
    public async Task Up_RefusesASizeLimitOnAQuestionOfAnotherType_AndAMinimumAboveTheMaximum()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(
            nameof(Up_RefusesASizeLimitOnAQuestionOfAnotherType_AndAMinimumAboveTheMaximum));
        await using var context = new QuestionBankModuleDbContext(
            new DbContextOptionsBuilder<QuestionBankModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
        await context.GetService<IMigrator>().MigrateAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        static string Insert(string type, string size) =>
            $$"""
            INSERT INTO {{Schema}}.questions (id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement, {{size.Split('=')[0]}}, created_at, is_deleted)
            VALUES ('{{Guid.CreateVersion7()}}', '{{type}}', 'Draft', '{{Guid.CreateVersion7()}}', 'pt-BR', '<p>x</p>', 'x', 'X', {{size.Split('=')[1]}}, now(), false)
            """;

        await ExecuteAsync(connection, Insert("Essay", "answer_size_max=30"));
        await ExecuteAsync(connection, Insert("OpenAnswer", "answer_size_max=5000"));
        (await ThrowsAsync(() => ExecuteAsync(connection, Insert("SingleChoice", "answer_size_max=30")))).Should().BeTrue("another type has no size limit");
        (await ThrowsAsync(() => ExecuteAsync(connection, Insert("Essay", "answer_size_max=101")))).Should().BeTrue("an essay has at most 100 lines");
        (await ThrowsAsync(() => ExecuteAsync(connection, Insert("OpenAnswer", "answer_size_min=5")))).Should().BeTrue("an open answer has no minimum");
    }

    private static async Task<bool> ThrowsAsync(Func<Task> action)
    {
        try
        {
            await action();

            return false;
        }
        catch (PostgresException)
        {
            return true;
        }
    }

    private static async Task<long> TableCountAsync(NpgsqlConnection connection) =>
        long.Parse((await ValuesAsync(
            connection,
            $"""
            SELECT count(*)::text FROM information_schema.tables
            WHERE table_schema = '{Schema}' AND table_name = 'question_rubric_criteria'
            """))[0], CultureInfo.InvariantCulture);

    private static Task<List<string>> TypeCommentsAsync(NpgsqlConnection connection) =>
        ValuesAsync(
            connection,
            $"""
            SELECT col_description(c.oid, a.attnum)
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_attribute a ON a.attrelid = c.oid
            WHERE n.nspname = '{Schema}' AND c.relname IN ('questions', 'question_versions') AND a.attname = 'type'
            """);

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> ValuesAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.IsDBNull(0) ? string.Empty : reader.GetString(0));
        }

        return values;
    }
}
