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
/// F-108 AC16: applied to a database that has a fill-in-the-blanks question with a blank and its version, the migration
/// only changes comments (the `type` columns name the sixth value, the blank descriptions no longer say a blank always
/// has a marker) and every row keeps its type, its blanks and its version.
/// </summary>
public sealed class AddShortAnswerQuestionsMigrationTests
{
    private const string PreviousMigration = "20261022105840_AddFillInTheBlanksQuestions";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_NamesTheSixthType_ChangesOnlyComments_AndKeepsEveryRow()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(
            nameof(Up_NamesTheSixthType_ChangesOnlyComments_AndKeepsEveryRow));
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
            VALUES ('{{questionId}}', 'FillInTheBlanks', 'Published', '{{Guid.CreateVersion7()}}', 'pt-BR', '<p>[[1]]</p>', '____', '____', now(), false);
            INSERT INTO {{Schema}}.question_blanks (id, question_id, number, position, accepted_answers, created_at, is_deleted)
            VALUES ('{{Guid.CreateVersion7()}}', '{{questionId}}', 1, 0, ARRAY['Brasília'], now(), false);
            INSERT INTO {{Schema}}.question_versions (id, question_id, number, type, content_language, statement_html, options, match_items, blanks, answer_key_changed, annulled, created_at, is_deleted)
            VALUES ('{{Guid.CreateVersion7()}}', '{{questionId}}', 1, 'FillInTheBlanks', 'pt-BR', '<p>[[1]]</p>', '[]'::jsonb, '[]'::jsonb, '[{"Number":1,"AcceptedAnswers":["Brasília"]}]'::jsonb, false, false, now(), false);
            """);

        (await TypeCommentsAsync(connection)).Should().OnlyContain(comment => !comment.Contains("ShortAnswer"));

        await migrator.MigrateAsync();

        var comments = await TypeCommentsAsync(connection);
        comments.Should().HaveCount(2);
        comments.Should().OnlyContain(comment => comment.Contains("FillInTheBlanks") && comment.Contains("ShortAnswer"));
        (await ValuesAsync(
                connection,
                $"""
                SELECT obj_description('{Schema}.question_blanks'::regclass)
                """))
            .Should().ContainSingle().Which.Should().NotContain("a marker [[n]] of the statement and the answers it accepts")
            .And.Contain("short-answer");
        (await ValuesAsync(connection, $"SELECT type FROM {Schema}.questions")).Should().Equal("FillInTheBlanks");
        (await ValuesAsync(connection, $"SELECT accepted_answers[1] FROM {Schema}.question_blanks")).Should().Equal("Brasília");

        var version = await context.QuestionVersions.AsNoTracking().SingleAsync();
        version.Number.Should().Be(1);
        version.Blanks.Should().ContainSingle().Which.AcceptedAnswers.Should().Equal("Brasília");
    }

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
