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
/// F-106 AC15: applied to a database that has a question and a version of an older type, the migration adds the
/// left-items table with its comments and the versions' JSON column, names the fourth type in the type comments, and
/// every row keeps its type and its options; the old version reads with no left items (system-design R1).
/// </summary>
public sealed class AddMatchingQuestionsMigrationTests
{
    private const string PreviousMigration = "20261021114811_AddQuestionImages";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_AddsTheMatchItemsTableAndColumn_NamesTheFourthType_AndKeepsEveryRow()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(
            nameof(Up_AddsTheMatchItemsTableAndColumn_NamesTheFourthType_AndKeepsEveryRow));
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
            $"""
            INSERT INTO {Schema}.questions (id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement, created_at, is_deleted)
            VALUES ('{questionId}', 'MultipleAnswer', 'Published', '{Guid.CreateVersion7()}', 'pt-BR', '<p>x</p>', 'x', 'x', now(), false);
            INSERT INTO {Schema}.question_options (id, question_id, position, text_html, is_correct, created_at, is_deleted)
            VALUES ('{Guid.CreateVersion7()}', '{questionId}', 0, '<p>A</p>', true, now(), false);
            INSERT INTO {Schema}.question_versions (id, question_id, number, type, content_language, statement_html, options, answer_key_changed, annulled, created_at, is_deleted)
            VALUES ('{Guid.CreateVersion7()}', '{questionId}', 1, 'MultipleAnswer', 'pt-BR', '<p>x</p>', '[]'::jsonb, false, false, now(), false);
            """);

        await migrator.MigrateAsync();

        (await ValuesAsync(connection, $"SELECT obj_description('{Schema}.question_match_items'::regclass)"))
            .Should().ContainSingle().Which.Should().NotBeNullOrWhiteSpace();
        (await ValuesAsync(
                connection,
                $"""
                SELECT a.attname FROM pg_attribute a
                WHERE a.attrelid = '{Schema}.question_match_items'::regclass AND a.attnum > 0 AND NOT a.attisdropped
                  AND col_description(a.attrelid, a.attnum) IS NULL
                """))
            .Should().BeEmpty("every column of the new table has its comment");
        (await ValuesAsync(connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'question_match_items'"))
            .Should().Contain("ix_question_match_items_question");
        (await ValuesAsync(
                connection,
                $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'question_versions' AND column_name = 'match_items'"))
            .Should().ContainSingle();
        (await ValuesAsync(
                connection,
                $"""
                SELECT col_description(c.oid, a.attnum)
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                JOIN pg_attribute a ON a.attrelid = c.oid
                WHERE n.nspname = '{Schema}' AND c.relname IN ('questions', 'question_versions') AND a.attname = 'type'
                """))
            .Should().HaveCount(2).And.OnlyContain(comment => comment.Contains("Matching"));
        (await ValuesAsync(connection, $"SELECT type FROM {Schema}.questions")).Should().Equal("MultipleAnswer");
        (await ValuesAsync(connection, $"SELECT text_html FROM {Schema}.question_options")).Should().Equal("<p>A</p>");

        var version = await context.QuestionVersions.AsNoTracking().SingleAsync();
        version.Number.Should().Be(1);
        version.MatchItems.Should().BeEmpty();
    }

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
