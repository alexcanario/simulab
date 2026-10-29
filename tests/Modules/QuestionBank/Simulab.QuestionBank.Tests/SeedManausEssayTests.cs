using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Text;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-163: the essay question of the Manaus 2026 paper the <c>SeedManausEssayQuestion</c> data migration writes, read from
/// the database the Api host migrated at its start (an empty database, AC1). The tests share that database and only read it.
/// </summary>
public sealed class SeedManausEssayTests : QuestionReviewApiTests
{
    internal static readonly Guid EssayId = SeedManausGuardQuestionsTests.QuestionId(61);
    internal static readonly Guid EssayBaseTextId = SeedManausGuardQuestionsTests.Id(13, 2);
    internal const string BaseTextTitle = "Prova discursiva: textos motivadores";

    // The grid of the exam board, written here on its own so a slip in the migration shows up: maximum points in order,
    // and, for the three micro-structural criteria, the per-error deduction their description must state.
    private static readonly int[] MaxPoints = [4, 4, 3, 2, 3, 4];
    private static readonly string?[] Deductions = [null, null, null, "0,50", "0,25", "0,25"];

    [Fact]
    public async Task Migrate_EmptyDatabase_TheEditionHoldsTheEssayAsQuestion61_AndTheOtherSixtyAreUnchanged()
    {
        var essay = (await EssayRowsAsync()).Should().ContainSingle().Subject;

        essay.PaperNumber.Should().Be(61);
        essay.ExamEditionId.Should().Be(SeedManausGuardQuestionsTests.Manaus);
        essay.Type.Should().Be(nameof(QuestionType.Essay));
        essay.Status.Should().Be(nameof(QuestionStatus.Published));
        essay.ContentLanguage.Should().Be("pt-BR");
        essay.TopicId.Should().Be(SeedManausGuardQuestionsTests.Id(7, 2));
        essay.NoticeSubjectId.Should().BeNull();
        essay.TenantId.Should().BeNull();
        essay.AnswerSizeMin.Should().Be(20);
        essay.AnswerSizeMax.Should().Be(30);
        essay.BaseTextId.Should().Be(EssayBaseTextId);

        (await CountAsync($"SELECT count(*) FROM question_bank.questions WHERE exam_edition_id = '{SeedManausGuardQuestionsTests.Manaus}' AND type <> 'Essay'"))
            .Should().Be(SeedManausGuardQuestionsTests.Expected.Length);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_TheEssayIsWhatTheDomainAndTheSanitizerMake()
    {
        var sanitizer = Factory.Services.GetRequiredService<QuestionHtmlSanitizer>();
        var essay = (await EssayRowsAsync()).Single();

        var statement = sanitizer.Sanitize(essay.StatementHtml);
        statement.Html.Should().Be(essay.StatementHtml);
        essay.StatementText.Should().Be(statement.PlainText);
        essay.NormalizedStatement.Should().Be(ComparableText.Normalize(statement.PlainText));
        essay.StatementText.Should().Contain("A ressignificação da Guarda Municipal");

        var baseText = await BaseTextRowAsync();
        var html = sanitizer.Sanitize(baseText.TextHtml);
        html.Html.Should().Be(baseText.TextHtml);
        baseText.NormalizedTitle.Should().Be(ComparableText.Normalize(baseText.Title));
        BaseText.Create(baseText.Title, baseText.ContentLanguage, html).IsSuccess.Should().BeTrue();

        var rubric = await RubricRowsAsync();
        var created = Question.Create(new QuestionContent(
            essay.TopicId, essay.ContentLanguage, statement, essay.BaseTextId, SanitizedHtml.Empty, null, [],
            essay.ExamEditionId, essay.PaperNumber, essay.NoticeSubjectId, QuestionType.Essay,
            Rubric: [.. rubric.Select(criterion => new CriterionDraft(criterion.Name, criterion.Description, criterion.MaxPoints))],
            AnswerSizeMin: essay.AnswerSizeMin,
            AnswerSizeMax: essay.AnswerSizeMax));
        created.IsSuccess.Should().BeTrue(created.Error?.Code);
        created.Value!.Publish().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_TheRubricIsTheSixCriteriaOfTheGrid_WithTheDeductionInTheDescription()
    {
        var rubric = await RubricRowsAsync();

        rubric.Select(criterion => criterion.Position).Should().Equal([1, 2, 3, 4, 5, 6]);
        rubric.Select(criterion => criterion.MaxPoints).Should().Equal(MaxPoints);
        rubric.Sum(criterion => criterion.MaxPoints).Should().Be(20);
        rubric.Should().OnlyContain(criterion => criterion.TenantId == null
            && criterion.Name.Length > 0 && criterion.Name.Length <= QuestionLimits.CriterionNameMaxLength
            && (criterion.Description == null || criterion.Description.Length <= QuestionLimits.CriterionDescriptionMaxLength));
        rubric.Select(criterion => criterion.Name).Should().OnlyHaveUniqueItems();
        for (var index = 3; index < 6; index++)
        {
            rubric[index].Description.Should().Contain(Deductions[index]!, $"criterion {index + 1} states its deduction per error");
        }
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_TheEssayHasOneVersionThatHoldsItsRubricAndBaseText_AndNoReviewEntry()
    {
        var versions = await QueryAsync(context => context.QuestionVersions.IgnoreQueryFilters()
            .Where(version => version.QuestionId == EssayId).OrderBy(version => version.Number).ToListAsync());
        var version = versions.Should().ContainSingle().Subject;
        var rubric = await RubricRowsAsync();
        var baseText = await BaseTextRowAsync();

        version.Number.Should().Be(1);
        version.Type.Should().Be(QuestionType.Essay);
        version.Annulled.Should().BeFalse();
        version.AnswerKeyChanged.Should().BeFalse();
        version.TenantId.Should().BeNull();
        version.AnswerSizeMin.Should().Be(20);
        version.AnswerSizeMax.Should().Be(30);
        version.BaseTextHtml.Should().Be(baseText.TextHtml);
        version.Rubric.Select(criterion => (criterion.Position, criterion.Name, criterion.Description, criterion.MaxPoints))
            .Should().Equal(rubric.Select(criterion => (criterion.Position, criterion.Name, criterion.Description, criterion.MaxPoints)));

        (await QueryAsync(context => context.QuestionReviewEntries.IgnoreQueryFilters().CountAsync(entry => entry.QuestionId == EssayId)))
            .Should().Be(0);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_TheBaseTextHoldsTheThreeTextsWithTheirSources()
    {
        var baseText = await BaseTextRowAsync();

        baseText.Title.Should().Be(BaseTextTitle);
        baseText.ContentLanguage.Should().Be("pt-BR");
        baseText.TenantId.Should().BeNull();
        baseText.TextHtml.Should().Contain("Texto I")
            .And.Contain("Texto II")
            .And.Contain("Texto III")
            .And.Contain("O perfil profissional do Guarda Municipal")
            .And.Contain("PEREIRA, M.")
            .And.Contain("Lei nº 13.675, de 11 de junho de 2018")
            .And.Contain("XVI.")
            .And.Contain("Guarda Municipal: segurança pública e o direito coletivo ao bem-estar")
            .And.Contain("GOMES, N.");
    }

    [Fact]
    public async Task Migrate_ApiHost_ServesTheOpenApiDocument_AndHasNoPendingModelChange()
    {
        (await EssayRowsAsync()).Should().HaveCount(1, "the host start ran the seed");

        using var response = await Client().GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await QueryAsync(context => Task.FromResult(context.Database.HasPendingModelChanges()))).Should().BeFalse();
    }

    private async Task<List<EssayRow>> EssayRowsAsync()
    {
        _ = Client();

        return await ReadAsync(
            $"""
            SELECT id, paper_number, topic_id, notice_subject_id, exam_edition_id, base_text_id, tenant_id, type, status,
                   content_language, statement_html, statement_text, normalized_statement, answer_size_min, answer_size_max
            FROM question_bank.questions WHERE exam_edition_id = '{SeedManausGuardQuestionsTests.Manaus}' AND type = 'Essay'
            """,
            reader => new EssayRow(
                reader.GetGuid(0),
                reader.GetInt32(1),
                reader.GetGuid(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.GetGuid(4),
                reader.IsDBNull(5) ? null : reader.GetGuid(5),
                reader.IsDBNull(6) ? null : reader.GetGuid(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetString(10),
                reader.GetString(11),
                reader.GetString(12),
                reader.IsDBNull(13) ? null : reader.GetInt32(13),
                reader.IsDBNull(14) ? null : reader.GetInt32(14)));
    }

    private async Task<List<RubricRow>> RubricRowsAsync()
    {
        _ = Client();

        return await ReadAsync(
            $"SELECT position, name, description, max_points, tenant_id FROM question_bank.question_rubric_criteria WHERE question_id = '{EssayId}' AND is_deleted = false ORDER BY position",
            reader => new RubricRow(
                reader.GetInt32(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetGuid(4)));
    }

    private async Task<BaseTextRow> BaseTextRowAsync()
    {
        _ = Client();

        return (await ReadAsync(
            $"SELECT id, title, normalized_title, content_language, text_html, tenant_id FROM question_bank.base_texts WHERE id = '{EssayBaseTextId}'",
            reader => new BaseTextRow(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetGuid(5)))).Single();
    }

    private async Task<long> CountAsync(string sql)
    {
        _ = Client();
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<List<T>> ReadAsync<T>(string sql, Func<NpgsqlDataReader, T> map)
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<T>();
        while (await reader.ReadAsync())
        {
            rows.Add(map(reader));
        }

        return rows;
    }

    private sealed record EssayRow(
        Guid Id,
        int PaperNumber,
        Guid TopicId,
        Guid? NoticeSubjectId,
        Guid ExamEditionId,
        Guid? BaseTextId,
        Guid? TenantId,
        string Type,
        string Status,
        string ContentLanguage,
        string StatementHtml,
        string StatementText,
        string NormalizedStatement,
        int? AnswerSizeMin,
        int? AnswerSizeMax);

    private sealed record RubricRow(int Position, string Name, string? Description, int MaxPoints, Guid? TenantId);

    private sealed record BaseTextRow(Guid Id, string Title, string NormalizedTitle, string ContentLanguage, string TextHtml, Guid? TenantId);
}
