using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-110 AC10 (UC4, BR9): a curator's edit of a seeded question survives the migrations being applied again and the seed
/// being rolled back; the rows nobody changed leave with the rollback. The edit goes through the Api, as a curator's would.
/// </summary>
public sealed class SeedManausGuardQuestionsRollbackTests : QuestionBankApiTests
{
    private const string EditedStatement = "<p>Enunciado revisado pela curadoria.</p>";

    [Fact]
    public async Task ACuratorsEdit_StaysWhenMigratingAgain_AndTheRollbackRemovesOnlyTheUntouchedRows()
    {
        var curator = await CuratorAsync();
        var edited = SeedManausGuardQuestionsTests.QuestionId(9);
        var seeded = await GetAsync(curator, edited);
        var request = new SaveQuestionRequest(
            seeded.TopicId,
            seeded.ContentLanguage,
            EditedStatement,
            null,
            null,
            null,
            [.. seeded.Options.Select(option => new SaveQuestionOptionRequest(option.TextHtml, option.IsCorrect))],
            seeded.Origin!.ExamEditionId,
            seeded.Origin.PaperNumber,
            seeded.Origin.NoticeSubjectId);
        using var update = await curator.PutAsJsonAsync($"{Questions}/{edited}", request, AppJson.Options);
        update.StatusCode.Should().Be(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());

        await using var context = SeedManausGuardQuestionsMigrationTests.QuestionBankContext(Factory.ConnectionString);
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync();

        (await CountAsync($"SELECT count(*) FROM question_bank.questions WHERE exam_edition_id = '{SeedManausGuardQuestionsTests.Manaus}' AND type <> 'Essay'"))
            .Should().Be(SeedManausGuardQuestionsTests.Expected.Length);
        (await GetAsync(curator, edited)).StatementHtml.Should().Be(EditedStatement);

        await migrator.MigrateAsync(SeedManausGuardQuestionsMigrationTests.PreviousMigration);

        (await ValuesAsync($"SELECT id::text FROM question_bank.questions WHERE exam_edition_id = '{SeedManausGuardQuestionsTests.Manaus}'"))
            .Should().Equal(edited.ToString());
        (await GetAsync(curator, edited)).StatementHtml.Should().Be(EditedStatement);
        (await CountAsync($"SELECT count(*) FROM question_bank.question_options WHERE question_id = '{edited}' AND is_deleted = false")).Should().Be(4);
        (await CountAsync($"SELECT count(*) FROM question_bank.question_versions WHERE question_id = '{edited}'")).Should().Be(2);
        (await CountAsync($"SELECT count(*) FROM question_bank.question_options WHERE question_id = '{SeedManausGuardQuestionsTests.QuestionId(10)}'")).Should().Be(0);
        (await CountAsync($"SELECT count(*) FROM question_bank.question_versions WHERE question_id = '{SeedManausGuardQuestionsTests.QuestionId(47)}'")).Should().Be(0);
        (await CountAsync($"SELECT count(*) FROM question_bank.base_texts WHERE id = '{SeedManausGuardQuestionsTests.Id(13, 1)}'")).Should().Be(0);
        (await CountAsync("SELECT count(*) FROM question_bank.question_images WHERE id::text LIKE '0198f370-0014-%'")).Should().Be(0);
        (await context.Database.GetAppliedMigrationsAsync()).Should().NotContain(name => name.EndsWith("SeedManausGuardQuestions", StringComparison.Ordinal));
    }

    private async Task<long> CountAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<List<string>> ValuesAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
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
