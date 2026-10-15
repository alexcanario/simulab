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
/// F-104 AC14: applied to a database that has questions of the two older types, the migration only updates the
/// comment of the two `type` columns to name the third value, and every question keeps its type and its options.
/// </summary>
public sealed class UpdateQuestionTypeCommentMigrationTests
{
    private const string PreviousMigration = "20261015193347_AddBaseTexts";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_NamesTheThirdTypeInBothTypeComments_AndKeepsEveryQuestionWithItsType()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(
            nameof(Up_NamesTheThirdTypeInBothTypeComments_AndKeepsEveryQuestionWithItsType));
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
        await InsertQuestionAsync(connection, "SingleChoice");
        await InsertQuestionAsync(connection, "TrueFalse");

        (await CommentsAsync(connection)).Should().OnlyContain(comment => !comment.Contains("MultipleAnswer"));

        await migrator.MigrateAsync();

        var comments = await CommentsAsync(connection);
        comments.Should().HaveCount(2);
        comments.Should().OnlyContain(comment =>
            comment.Contains("SingleChoice") && comment.Contains("TrueFalse") && comment.Contains("MultipleAnswer"));
        (await ValuesAsync(connection, $"SELECT type FROM {Schema}.questions ORDER BY type"))
            .Should().Equal("SingleChoice", "TrueFalse");
    }

    private static async Task InsertQuestionAsync(NpgsqlConnection connection, string type)
    {
        const string columns =
            "id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement, created_at, is_deleted";
        const string values = "@id, @type, 'Draft', @topic, 'pt-BR', '<p>x</p>', 'x', 'x', now(), false";

        await using var command = new NpgsqlCommand($"INSERT INTO {Schema}.questions ({columns}) VALUES ({values})", connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("type", type);
        command.Parameters.AddWithValue("topic", Guid.CreateVersion7());
        await command.ExecuteNonQueryAsync();
    }

    private static Task<List<string>> CommentsAsync(NpgsqlConnection connection) =>
        ValuesAsync(
            connection,
            $"""
            SELECT col_description(c.oid, a.attnum)
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_attribute a ON a.attrelid = c.oid
            WHERE n.nspname = '{Schema}' AND c.relname IN ('questions', 'question_versions') AND a.attname = 'type'
            """);

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
}
