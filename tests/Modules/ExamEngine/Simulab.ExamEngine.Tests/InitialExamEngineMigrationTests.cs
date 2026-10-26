using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.ExamEngine.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;
using Simulab.Testing;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 AC20: the initial migration creates the schema, the two tables, the rule that allows one attempt in progress
/// per student and edition, and the check between the state and the submission time, in a real PostgreSQL; a second run
/// changes nothing and going back drops them. Nothing points at another module's table with a foreign key.
/// </summary>
public sealed class InitialExamEngineMigrationTests
{
    private const string Schema = ExamEngineModuleDbContext.SchemaName;

    private static readonly Guid Student = Guid.Parse("0198f370-0098-7000-8000-000000000001");
    private static readonly Guid Edition = Guid.Parse("0198f370-0098-7000-8000-000000000002");

    private static async Task<(string ConnectionString, IMigrator Migrator)> MigratedAsync(string name)
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(name);
        var context = new ExamEngineModuleDbContext(
            new DbContextOptionsBuilder<ExamEngineModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync();

        return (connectionString, migrator);
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

    private static async Task<int> ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);

        return await command.ExecuteNonQueryAsync();
    }

    private static string InsertAttempt(Guid id, string status, string? submittedAt, string? tenantId = null) =>
        $"INSERT INTO {Schema}.attempts (id, tenant_id, student_id, exam_edition_id, status, started_at, submitted_at, created_at, is_deleted) " +
        $"VALUES ('{id}', {(tenantId is null ? "NULL" : $"'{tenantId}'")}, '{Student}', '{Edition}', '{status}', now(), {(submittedAt is null ? "NULL" : $"'{submittedAt}'")}, now(), false)";

    [Fact]
    public async Task Up_CreatesTheTablesIndexesAndChecks_AndDownDropsThem_AndASecondRunChangesNothing()
    {
        var (connectionString, migrator) = await MigratedAsync(nameof(Up_CreatesTheTablesIndexesAndChecks_AndDownDropsThem_AndASecondRunChangesNothing));
        await migrator.MigrateAsync();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        (await ValuesAsync(connection, $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}' AND table_name <> '__ef_migrations_history'"))
            .Should().BeEquivalentTo("attempts", "attempt_answers");
        (await ValuesAsync(connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'attempts'"))
            .Should().Contain(["pk_attempts", "ux_attempts_tenant_student_edition_in_progress"]);
        (await ValuesAsync(connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'attempt_answers'"))
            .Should().Contain(["pk_attempt_answers", "ux_attempt_answers_tenant_attempt_question", "ix_attempt_answers_attempt"]);
        (await ValuesAsync(connection, $"SELECT conname FROM pg_constraint WHERE contype = 'f' AND connamespace = '{Schema}'::regnamespace"))
            .Should().Equal(["fk_attempt_answers_attempt"], "the student, the edition and the question are held by id, never by a foreign key");
        (await ValuesAsync(connection, $"SELECT indexdef FROM pg_indexes WHERE schemaname = '{Schema}' AND indexname = 'ux_attempts_tenant_student_edition_in_progress'"))
            .Single().Should().Contain("NULLS NOT DISTINCT");
        (await ValuesAsync(connection, $"SELECT indexdef FROM pg_indexes WHERE schemaname = '{Schema}' AND indexname = 'ux_attempt_answers_tenant_attempt_question'"))
            .Single().Should().Contain("NULLS NOT DISTINCT");

        await migrator.MigrateAsync("0");

        (await ValuesAsync(connection, $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}' AND table_name <> '__ef_migrations_history'"))
            .Should().BeEmpty();
    }

    // AC20: two global attempts in progress for the same student and edition conflict; a submitted one never does.
    [Fact]
    public async Task TwoGlobalAttemptsInProgress_OfOneStudentAndEdition_Conflict_ButASubmittedOneDoesNot()
    {
        var (connectionString, _) = await MigratedAsync(nameof(TwoGlobalAttemptsInProgress_OfOneStudentAndEdition_Conflict_ButASubmittedOneDoesNot));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, InsertAttempt(Guid.CreateVersion7(), "InProgress", null));

        var duplicate = async () => await ExecuteAsync(connection, InsertAttempt(Guid.CreateVersion7(), "InProgress", null));
        (await duplicate.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);

        await ExecuteAsync(connection, InsertAttempt(Guid.CreateVersion7(), "Submitted", "2026-10-26 10:00:00+00"));
        await ExecuteAsync(connection, InsertAttempt(Guid.CreateVersion7(), "Submitted", "2026-10-26 11:00:00+00"));
    }

    // AC20: the state and the submission time agree, in the database as in the domain.
    [Theory]
    [InlineData("Submitted", null)]
    [InlineData("InProgress", "2026-10-26 10:00:00+00")]
    public async Task AnAttemptWhoseStateAndSubmissionTimeDisagree_IsRefused(string status, string? submittedAt)
    {
        var (connectionString, _) = await MigratedAsync($"{nameof(AnAttemptWhoseStateAndSubmissionTimeDisagree_IsRefused)}_{status}");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var insert = async () => await ExecuteAsync(connection, InsertAttempt(Guid.CreateVersion7(), status, submittedAt));

        (await insert.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    // AC20: one row per question of an attempt, also for global rows.
    [Fact]
    public async Task TwoGlobalAnswerRows_OfOneAttemptAndQuestion_Conflict()
    {
        var (connectionString, _) = await MigratedAsync(nameof(TwoGlobalAnswerRows_OfOneAttemptAndQuestion_Conflict));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var attempt = Guid.CreateVersion7();
        var question = Guid.CreateVersion7();
        await ExecuteAsync(connection, InsertAttempt(attempt, "InProgress", null));
        string Insert() =>
            $"INSERT INTO {Schema}.attempt_answers (id, tenant_id, attempt_id, question_id, paper_number, version_number, question_type, answer, created_at, is_deleted) " +
            $"VALUES ('{Guid.CreateVersion7()}', NULL, '{attempt}', '{question}', 1, 1, 'SingleChoice', NULL, now(), false)";
        await ExecuteAsync(connection, Insert());

        var duplicate = async () => await ExecuteAsync(connection, Insert());

        (await duplicate.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    // AC20: every column and the enum values are described.
    [Fact]
    public async Task EveryColumn_HasAComment_AndTheEnumColumnsListTheirValues()
    {
        var (connectionString, _) = await MigratedAsync(nameof(EveryColumn_HasAComment_AndTheEnumColumnsListTheirValues));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        (await ValuesAsync(connection,
            $"SELECT c.table_name || '.' || c.column_name FROM information_schema.columns c " +
            $"WHERE c.table_schema = '{Schema}' AND c.table_name IN ('attempts', 'attempt_answers') " +
            $"AND coalesce(col_description(('{Schema}.' || c.table_name)::regclass, c.ordinal_position), '') = ''"))
            .Should().BeEmpty("every column carries a comment");
        (await ValuesAsync(connection, $"SELECT col_description('{Schema}.attempts'::regclass, ordinal_position) FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'attempts' AND column_name = 'status'"))
            .Single().Should().Contain("InProgress").And.Contain("Submitted");
        (await ValuesAsync(connection, $"SELECT col_description('{Schema}.attempt_answers'::regclass, ordinal_position) FROM information_schema.columns WHERE table_schema = '{Schema}' AND table_name = 'attempt_answers' AND column_name = 'question_type'"))
            .Single().Should().ContainAll(Enum.GetNames<Simulab.ExamEngine.Contracts.AttemptQuestionType>());
    }
}
