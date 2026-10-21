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
/// F-95: the initial migration creates the schema, the two tables, their indexes and the difficulty check in
/// a real PostgreSQL, and going back drops them. Nothing in the module points at a catalog table with a
/// foreign key (BR13: the guard is a check, never a constraint).
/// </summary>
public sealed class InitialQuestionBankMigrationTests
{
    private const string Schema = QuestionBankModuleDbContext.SchemaName;

    [Fact]
    public async Task Up_CreatesTheTablesIndexesAndCheck_AndDownDropsThem()
    {
        var connectionString = await PostgresServer.CreateDatabaseAsync(nameof(InitialQuestionBankMigrationTests));
        await using var context = new QuestionBankModuleDbContext(
            new DbContextOptionsBuilder<QuestionBankModuleDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(Schema))
                .UseSnakeCaseNamingConvention()
                .Options,
            new NoTenant());
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        (await ValuesAsync(connection, $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}' AND table_name <> '__ef_migrations_history'"))
            .Should().BeEquivalentTo("questions", "question_options", "question_versions", "base_texts", "question_review_entries", "question_images");
        (await ValuesAsync(connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'questions'"))
            .Should().Contain(["pk_questions", "ix_questions_topic", "ix_questions_status_created_at"]);
        (await ValuesAsync(connection, $"SELECT indexname FROM pg_indexes WHERE schemaname = '{Schema}' AND tablename = 'question_options'"))
            .Should().Contain(["pk_question_options", "ix_question_options_question"]);
        (await ValuesAsync(connection, "SELECT conname FROM pg_constraint WHERE contype = 'c' AND conname = 'ck_questions_difficulty_range'"))
            .Should().ContainSingle();
        (await ValuesAsync(connection, $"SELECT conname FROM pg_constraint WHERE contype = 'f' AND conrelid = '{Schema}.questions'::regclass"))
            .Should().Equal(["fk_questions_base_text"], "a question holds its topic by id, never by a foreign key; the base text link of F-102 is inside the module");

        await migrator.MigrateAsync("0");

        (await ValuesAsync(connection, $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{Schema}' AND table_name <> '__ef_migrations_history'"))
            .Should().BeEmpty();
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
}
