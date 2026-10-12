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
/// F-96 AC1 and AC6: applied to a database that has the F-95 migration and questions in it, the new migration
/// adds the three columns, the check, the unique index and the edition index in a real PostgreSQL, leaves
/// every existing question authored, and the database itself refuses a repeated number.
/// </summary>
public sealed class AddQuestionOriginMigrationTests
{
    private const string PreviousMigration = "20261010155203_InitialQuestionBank";
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_AddsTheOriginColumnsCheckAndIndexes_AndKeepsEveryExistingQuestionAuthored()
    {
        await using var database = await MigratedAsync(nameof(Up_AddsTheOriginColumnsCheckAndIndexes_AndKeepsEveryExistingQuestionAuthored));
        await InsertQuestionAsync(database.Connection, null, null, null);

        await database.Migrator.MigrateAsync();

        (await ValuesAsync(database.Connection, $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'questions'"))
            .Should().Contain(["exam_edition_id", "paper_number", "notice_subject_id"]);
        (await ValuesAsync(database.Connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'questions'"))
            .Should().Contain(["ux_questions_tenant_edition_paper_number", "ix_questions_exam_edition", "ix_questions_notice_subject"]);
        (await ValuesAsync(database.Connection, "SELECT conname FROM pg_constraint WHERE contype = 'c' AND conname = 'ck_questions_origin'"))
            .Should().ContainSingle();
        (await ValuesAsync(database.Connection, $"SELECT conname FROM pg_constraint WHERE contype = 'f' AND conrelid = '{Schema}.questions'::regclass"))
            .Should().BeEmpty("a question holds its edition by id, never by a foreign key");
        (await ValuesAsync(database.Connection, $"SELECT count(*)::text FROM {Schema}.questions WHERE exam_edition_id IS NULL AND paper_number IS NULL AND notice_subject_id IS NULL"))
            .Should().Equal("1");

        await database.Migrator.MigrateAsync(PreviousMigration);

        (await ValuesAsync(database.Connection, $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'questions'"))
            .Should().NotContain("exam_edition_id");
    }

    [Fact]
    public async Task TheDatabase_RefusesARepeatedNumberInAnEdition_ButNotAnotherEditionOrADeletedRow()
    {
        await using var database = await MigratedAsync(nameof(TheDatabase_RefusesARepeatedNumberInAnEdition_ButNotAnotherEditionOrADeletedRow));
        await database.Migrator.MigrateAsync();
        var edition = Guid.CreateVersion7();
        await InsertQuestionAsync(database.Connection, edition, 12, null);

        var repeated = async () => await InsertQuestionAsync(database.Connection, edition, 12, null);

        (await repeated.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23505");
        await InsertQuestionAsync(database.Connection, Guid.CreateVersion7(), 12, null);
        await InsertQuestionAsync(database.Connection, edition, 13, null);
        await InsertQuestionAsync(database.Connection, edition, 12, null, isDeleted: true);

        // Two authored questions never collide: every origin column is null.
        await InsertQuestionAsync(database.Connection, null, null, null);
        await InsertQuestionAsync(database.Connection, null, null, null);
    }

    [Fact]
    public async Task TheDatabase_RefusesANumberOrASubjectWithNoEdition_AndAnEditionWithNoNumber()
    {
        await using var database = await MigratedAsync(nameof(TheDatabase_RefusesANumberOrASubjectWithNoEdition_AndAnEditionWithNoNumber));
        await database.Migrator.MigrateAsync();

        var numberOnly = async () => await InsertQuestionAsync(database.Connection, null, 5, null);
        var subjectOnly = async () => await InsertQuestionAsync(database.Connection, null, null, Guid.CreateVersion7());
        var editionOnly = async () => await InsertQuestionAsync(database.Connection, Guid.CreateVersion7(), null, null);
        var outOfRange = async () => await InsertQuestionAsync(database.Connection, Guid.CreateVersion7(), 501, null);

        foreach (var insert in new[] { numberOnly, subjectOnly, editionOnly, outOfRange })
        {
            (await insert.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23514");
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

    private static async Task InsertQuestionAsync(
        NpgsqlConnection connection,
        Guid? edition,
        int? number,
        Guid? subject,
        bool isDeleted = false)
    {
        var columns = "id, type, status, topic_id, content_language, statement_html, statement_text, normalized_statement, created_at, is_deleted";
        var values = "@id, 'SingleChoice', 'Draft', @topic, 'pt-BR', '<p>x</p>', 'x', 'x', now(), @deleted";
        var hasOrigin = await ColumnExistsAsync(connection, "exam_edition_id");
        if (hasOrigin)
        {
            columns += ", exam_edition_id, paper_number, notice_subject_id";
            values += ", @edition, @number, @subject";
        }

        await using var command = new NpgsqlCommand($"INSERT INTO {Schema}.questions ({columns}) VALUES ({values})", connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("topic", Guid.CreateVersion7());
        command.Parameters.AddWithValue("deleted", isDeleted);
        if (hasOrigin)
        {
            command.Parameters.AddWithValue("edition", (object?)edition ?? DBNull.Value);
            command.Parameters.AddWithValue("number", (object?)number ?? DBNull.Value);
            command.Parameters.AddWithValue("subject", (object?)subject ?? DBNull.Value);
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
