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
/// F-105 AC17: applied to a database with questions in every status, the migration adds the review history table
/// with its comments and index, names the new status in the status comment, and changes no stored row; going back
/// turns a question in review into a draft before the table is dropped.
/// </summary>
public sealed class AddQuestionReviewMigrationTests
{
    private const string PreviousMigration = "20261015215310_UpdateQuestionTypeComment";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    private static async Task<(QuestionBankModuleDbContext Context, IMigrator Migrator, NpgsqlConnection Connection)> OpenAtPreviousAsync(string name)
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

        return (context, migrator, connection);
    }

    [Fact]
    public async Task Up_AddsTheHistoryTableWithItsCommentsAndIndex_NamesInReviewInTheStatusComment_AndKeepsEveryStatus()
    {
        var (context, migrator, connection) = await OpenAtPreviousAsync(
            nameof(Up_AddsTheHistoryTableWithItsCommentsAndIndex_NamesInReviewInTheStatusComment_AndKeepsEveryStatus));
        await using var _ = context;
        await using var __ = connection;
        await InsertQuestionAsync(connection, "Draft");
        await InsertQuestionAsync(connection, "Published");
        await InsertQuestionAsync(connection, "Annulled");

        (await ValuesAsync(connection, TableExistsSql)).Should().Equal("0");

        await migrator.MigrateAsync();

        (await ValuesAsync(connection, TableExistsSql)).Should().Equal("1");
        (await ValuesAsync(connection, $"SELECT status FROM {Schema}.questions ORDER BY status")).Should().Equal("Annulled", "Draft", "Published");
        (await ValuesAsync(connection, ColumnCommentSql("questions", "status")))
            .Single().Should().Contain("Draft").And.Contain("InReview").And.Contain("Published").And.Contain("Annulled");
        (await ValuesAsync(connection, ColumnCommentSql("question_review_entries", "kind")))
            .Single().Should().Contain("Submitted").And.Contain("Withdrawn").And.Contain("Approved").And.Contain("Rejected");
        (await ValuesAsync(connection, $"""
            SELECT obj_description('{Schema}.question_review_entries'::regclass, 'pg_class')
            """)).Single().Should().NotBeNullOrWhiteSpace();
        (await ValuesAsync(connection, $"""
            SELECT indexname FROM pg_indexes
            WHERE schemaname = '{Schema}' AND tablename = 'question_review_entries'
            """)).Should().Contain("ix_question_review_entries_question_id_created_at");
        (await ValuesAsync(connection, $"""
            SELECT count(*) FROM information_schema.columns
            WHERE table_schema = '{Schema}' AND table_name = 'question_review_entries'
              AND column_name IN ('id', 'question_id', 'kind', 'reason', 'tenant_id', 'created_at', 'created_by')
            """)).Should().Equal("7");
        (await ValuesAsync(connection, $"SELECT count(*) FROM {Schema}.question_review_entries")).Should().Equal("0");
    }

    [Fact]
    public async Task Down_TurnsAQuestionInReviewBackIntoADraft_AndDropsTheTable()
    {
        var (context, migrator, connection) = await OpenAtPreviousAsync(
            nameof(Down_TurnsAQuestionInReviewBackIntoADraft_AndDropsTheTable));
        await using var _ = context;
        await using var __ = connection;
        await migrator.MigrateAsync();
        await InsertQuestionAsync(connection, "InReview");
        await InsertQuestionAsync(connection, "Published");

        await migrator.MigrateAsync(PreviousMigration);

        (await ValuesAsync(connection, TableExistsSql)).Should().Equal("0");
        (await ValuesAsync(connection, $"SELECT status FROM {Schema}.questions ORDER BY status")).Should().Equal("Draft", "Published");
        (await ValuesAsync(connection, ColumnCommentSql("questions", "status")))
            .Single().Should().NotContain("InReview");
    }

    private static string TableExistsSql => $"""
        SELECT count(*) FROM information_schema.tables
        WHERE table_schema = '{Schema}' AND table_name = 'question_review_entries'
        """;

    private static string ColumnCommentSql(string table, string column) => $"""
        SELECT col_description(c.oid, a.attnum)
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid
        WHERE n.nspname = '{Schema}' AND c.relname = '{table}' AND a.attname = '{column}'
        """;

    private static async Task InsertQuestionAsync(NpgsqlConnection connection, string status)
    {
        const string columns =
            "id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement, created_at, is_deleted";
        const string values = "@id, 'SingleChoice', @status, @topic, 'pt-BR', '<p>x</p>', 'x', 'x', now(), false";

        await using var command = new NpgsqlCommand($"INSERT INTO {Schema}.questions ({columns}) VALUES ({values})", connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("topic", Guid.CreateVersion7());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> ValuesAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(Convert.ToString(reader.GetValue(0), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return values;
    }
}
