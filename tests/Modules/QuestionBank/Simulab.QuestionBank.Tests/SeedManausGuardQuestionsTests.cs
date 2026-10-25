using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.QuestionBank.Application.Content;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.QuestionBank.Infrastructure.Images;
using Simulab.SharedKernel.Text;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-110: the questions of the Manaus 2026 municipal guard paper the <c>SeedManausGuardQuestions</c> data migration
/// writes, read from the database the Api host migrated at its start (an empty database, AC1). The tests share that
/// database and only read it.
/// </summary>
public sealed class SeedManausGuardQuestionsTests : QuestionReviewApiTests
{
    internal static readonly Guid Manaus = Id(4, 2);
    internal const string AnnulmentReason = "Anulada pela banca no gabarito definitivo.";

    // The approved classification of F-110, written here on its own so a slip in the migration shows up:
    // paper number, F-51 topic number, F-78 notice subject number, key (question 47 is seeded with D, then annulled).
    internal static readonly (int Number, int Topic, int NoticeSubject, char Key)[] Expected =
    [
        (1, 1, 7, 'B'), (2, 1, 7, 'B'), (3, 5, 7, 'B'), (4, 1, 7, 'D'), (5, 5, 7, 'D'),
        (6, 1, 7, 'B'), (7, 6, 7, 'A'), (8, 1, 7, 'D'), (9, 3, 7, 'D'), (10, 3, 7, 'D'),
        (11, 24, 8, 'D'), (12, 25, 8, 'D'), (13, 25, 8, 'A'), (14, 28, 8, 'C'), (15, 27, 8, 'D'),
        (16, 66, 9, 'B'), (17, 66, 9, 'B'), (18, 66, 9, 'D'), (19, 66, 9, 'A'), (20, 66, 9, 'A'),
        (21, 46, 10, 'A'), (22, 47, 10, 'D'), (23, 47, 10, 'B'), (24, 47, 10, 'A'), (25, 48, 10, 'D'),
        (26, 35, 11, 'C'), (27, 35, 11, 'C'), (28, 38, 11, 'C'), (29, 38, 11, 'D'), (30, 36, 11, 'D'),
        (31, 43, 12, 'C'), (32, 43, 12, 'C'), (33, 39, 12, 'B'), (34, 43, 12, 'C'), (35, 43, 12, 'D'),
        (36, 44, 13, 'D'), (37, 45, 13, 'C'), (38, 44, 13, 'D'), (39, 45, 13, 'D'), (40, 45, 13, 'D'),
        (41, 57, 14, 'C'), (42, 57, 14, 'D'), (43, 57, 14, 'A'), (44, 57, 14, 'D'), (45, 57, 14, 'D'),
        (46, 50, 15, 'C'), (47, 51, 15, 'D'), (48, 43, 15, 'D'), (49, 55, 15, 'C'), (50, 51, 15, 'D'),
        (51, 43, 15, 'C'), (52, 58, 15, 'D'), (53, 43, 15, 'C'), (54, 43, 15, 'D'), (55, 59, 15, 'B'),
        (56, 31, 15, 'B'), (57, 56, 15, 'C'), (58, 31, 15, 'B'), (59, 31, 15, 'B'), (60, 52, 15, 'B'),
    ];

    internal static Guid Id(int group, int number) => new($"0198f370-{group:D4}-7000-8000-{number:D12}");

    internal static Guid QuestionId(int number) => Id(10, number);

    [Fact]
    public async Task Migrate_EmptyDatabase_TheEditionHoldsExactlyTheClassifiedQuestionsWithTheirKeys()
    {
        var rows = await QuestionRowsAsync();

        rows.Select(row => row.PaperNumber).Should().Equal(Expected.Select(row => (int?)row.Number));
        foreach (var (number, topic, noticeSubject, key) in Expected)
        {
            var row = rows.Single(question => question.PaperNumber == number);
            row.Id.Should().Be(QuestionId(number));
            row.TopicId.Should().Be(Id(7, topic), $"question {number}");
            row.NoticeSubjectId.Should().Be(Id(8, noticeSubject), $"question {number}");
            row.TenantId.Should().BeNull();
            row.Type.Should().Be(nameof(QuestionType.SingleChoice));
            row.ContentLanguage.Should().Be("pt-BR");

            var options = await OptionRowsAsync(row.Id);
            options.Select(option => option.Position).Should().Equal([0, 1, 2, 3], $"question {number}");
            options.Where(option => option.IsCorrect).Select(option => option.Position)
                .Should().Equal([key - 'A'], $"question {number}");
            options.Should().OnlyContain(option => option.TenantId == null && option.TextHtml.Length > 0);
        }
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EverySeededQuestionIsWhatTheDomainAndTheSanitizerMake()
    {
        var sanitizer = Factory.Services.GetRequiredService<QuestionHtmlSanitizer>();
        var rows = await QuestionRowsAsync();
        rows.Should().HaveCount(Expected.Length);

        foreach (var row in rows)
        {
            var statement = sanitizer.Sanitize(row.StatementHtml);
            statement.Html.Should().Be(row.StatementHtml, $"question {row.PaperNumber} is stored as the sanitizer returns it");
            row.StatementText.Should().Be(statement.PlainText, $"question {row.PaperNumber}");
            row.NormalizedStatement.Should().Be(ComparableText.Normalize(statement.PlainText), $"question {row.PaperNumber}");

            var options = await OptionRowsAsync(row.Id);
            var drafts = new List<OptionDraft>();
            foreach (var option in options)
            {
                var text = sanitizer.Sanitize(option.TextHtml);
                text.Html.Should().Be(option.TextHtml, $"question {row.PaperNumber}, option {option.Position}");
                drafts.Add(new OptionDraft(text, option.IsCorrect));
            }

            var created = Question.Create(new QuestionContent(
                row.TopicId, row.ContentLanguage, statement, row.BaseTextId, SanitizedHtml.Empty, null, drafts,
                row.ExamEditionId, row.PaperNumber, row.NoticeSubjectId));
            created.IsSuccess.Should().BeTrue($"question {row.PaperNumber}: {created.Error?.Code}");
            var published = created.Value!.Publish();
            published.IsSuccess.Should().BeTrue($"question {row.PaperNumber}: {published.Error?.Code}");
        }

        var baseText = await BaseTextRowAsync();
        var html = sanitizer.Sanitize(baseText.TextHtml);
        html.Html.Should().Be(baseText.TextHtml);
        baseText.NormalizedTitle.Should().Be(ComparableText.Normalize(baseText.Title));
        BaseText.Create(baseText.Title, baseText.ContentLanguage, html).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_EveryQuestionBut47IsPublishedWithOneVersionThatHoldsItsOptions_AndNoReviewEntry()
    {
        foreach (var (number, _, _, _) in Expected.Where(row => row.Number != 47))
        {
            var id = QuestionId(number);
            (await StatusOfAsync(id)).Should().Be(nameof(QuestionStatus.Published), $"question {number}");

            var versions = await VersionsAsync(id);
            versions.Should().ContainSingle($"question {number}");
            var version = versions[0];
            version.Number.Should().Be(1);
            version.Annulled.Should().BeFalse();
            version.AnswerKeyChanged.Should().BeFalse();
            version.Reason.Should().BeNull();
            version.Type.Should().Be(QuestionType.SingleChoice);
            version.ContentLanguage.Should().Be("pt-BR");
            version.TenantId.Should().BeNull();

            var row = (await QuestionRowsAsync()).Single(question => question.Id == id);
            version.StatementHtml.Should().Be(row.StatementHtml);
            var options = await OptionRowsAsync(id);
            version.Options.Select(option => option.Position).Should().Equal(options.Select(option => option.Position));
            version.Options.Select(option => option.TextHtml).Should().Equal(options.Select(option => option.TextHtml));
            version.Options.Select(option => option.IsCorrect).Should().Equal(options.Select(option => option.IsCorrect));
        }

        var questionIds = Expected.Select(row => QuestionId(row.Number)).ToList();
        (await QueryAsync(context => context.QuestionReviewEntries.IgnoreQueryFilters().CountAsync(entry => questionIds.Contains(entry.QuestionId))))
            .Should().Be(0);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_Question47IsAnnulledWithKeyDThenTheBoardsReason()
    {
        var id = QuestionId(47);

        (await StatusOfAsync(id)).Should().Be(nameof(QuestionStatus.Annulled));
        var versions = await VersionsAsync(id);
        versions.Select(version => version.Number).Should().Equal([1, 2]);

        var first = versions[0];
        first.Annulled.Should().BeFalse();
        first.AnswerKeyChanged.Should().BeFalse();
        first.Reason.Should().BeNull();
        first.Options.Where(option => option.IsCorrect).Select(option => option.Position).Should().Equal([3]);

        var second = versions[1];
        second.Annulled.Should().BeTrue();
        second.AnswerKeyChanged.Should().BeFalse();
        second.Reason.Should().Be(AnnulmentReason);
        second.StatementHtml.Should().Be(first.StatementHtml);
        second.Options.Select(option => (option.Position, option.IsCorrect))
            .Should().Equal(first.Options.Select(option => (option.Position, option.IsCorrect)));
        second.Options.Select(option => option.TextHtml).Should().Equal(first.Options.Select(option => option.TextHtml));
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_Questions01To08ShareTheBaseTextAndTheirVersionsHoldItsHtml()
    {
        var baseText = await BaseTextRowAsync();
        baseText.Id.Should().Be(Id(13, 1));
        baseText.Title.Should().Be("Calma, gente");
        baseText.ContentLanguage.Should().Be("pt-BR");
        baseText.TenantId.Should().BeNull();

        var rows = await QuestionRowsAsync();
        rows.Where(row => row.PaperNumber <= 8).Should().OnlyContain(row => row.BaseTextId == baseText.Id).And.HaveCount(8);
        rows.Where(row => row.PaperNumber > 8).Should().OnlyContain(row => row.BaseTextId == null);

        foreach (var number in Enumerable.Range(1, 8))
        {
            (await VersionsAsync(QuestionId(number))).Should().OnlyContain(version => version.BaseTextHtml == baseText.TextHtml);
        }

        (await VersionsAsync(QuestionId(9))).Should().OnlyContain(version => version.BaseTextHtml == null);
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_TheThreeImagesAreOwnedByQuestions16And17_AndTheSweepKeepsThem()
    {
        var images = await ImageRowsAsync();

        images.Select(image => image.Id).Should().Equal([Id(14, 1), Id(14, 2), Id(14, 3)]);
        images.Select(image => image.OwnerId).Should().Equal([QuestionId(16), QuestionId(16), QuestionId(17)]);
        images.Should().OnlyContain(image => image.OwnerKind == nameof(QuestionImageOwnerKind.Question)
            && image.ContentType == "image/png"
            && image.BlobName == $"question-images/{image.Id}"
            && !image.IsDeleted
            && image.TenantId == null);
        images.Select(image => image.SizeBytes).Should().Equal([119641L, 143978L, 131836L]);

        var rows = await QuestionRowsAsync();
        var sixteen = rows.Single(row => row.PaperNumber == 16).StatementHtml;
        sixteen.Should().Contain($"/question-images/{Id(14, 1)}").And.Contain($"/question-images/{Id(14, 2)}");
        rows.Single(row => row.PaperNumber == 17).StatementHtml.Should().Contain($"/question-images/{Id(14, 3)}");

        Factory.Clock.Advance(TimeSpan.FromHours(25));
        var removed = await Factory.Services.GetRequiredService<QuestionImageSweep>().RunAsync();

        removed.Should().Be(0);
        (await ImageRowsAsync()).Should().HaveCount(3).And.OnlyContain(image => !image.IsDeleted);
    }

    [Fact]
    public async Task Migrate_ApiHost_ServesTheOpenApiDocument_AndHasNoPendingModelChange()
    {
        (await QuestionRowsAsync()).Should().HaveCount(Expected.Length, "the host start ran the seed");

        using var response = await Client().GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await QueryAsync(context => Task.FromResult(context.Database.HasPendingModelChanges()))).Should().BeFalse();
    }

    private async Task<List<QuestionRow>> QuestionRowsAsync()
    {
        _ = Client();

        return await ReadAsync(
            $"""
            SELECT id, paper_number, topic_id, notice_subject_id, exam_edition_id, base_text_id, tenant_id, type,
                   content_language, statement_html, statement_text, normalized_statement
            FROM question_bank.questions WHERE exam_edition_id = '{Manaus}' ORDER BY paper_number
            """,
            reader => new QuestionRow(
                reader.GetGuid(0),
                reader.IsDBNull(1) ? null : reader.GetInt32(1),
                reader.GetGuid(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.IsDBNull(4) ? null : reader.GetGuid(4),
                reader.IsDBNull(5) ? null : reader.GetGuid(5),
                reader.IsDBNull(6) ? null : reader.GetGuid(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetString(10),
                reader.GetString(11)));
    }

    private Task<List<OptionRow>> OptionRowsAsync(Guid questionId) =>
        ReadAsync(
            $"SELECT position, text_html, is_correct, tenant_id FROM question_bank.question_options WHERE question_id = '{questionId}' AND is_deleted = false ORDER BY position",
            reader => new OptionRow(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2), reader.IsDBNull(3) ? null : reader.GetGuid(3)));

    private async Task<BaseTextRow> BaseTextRowAsync()
    {
        _ = Client();

        return (await ReadAsync(
            $"SELECT id, title, normalized_title, content_language, text_html, tenant_id FROM question_bank.base_texts WHERE id = '{Id(13, 1)}'",
            reader => new BaseTextRow(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetGuid(5)))).Single();
    }

    private async Task<List<ImageRow>> ImageRowsAsync()
    {
        _ = Client();

        return await ReadAsync(
            "SELECT id, blob_name, content_type, size_bytes, owner_kind, owner_id, is_deleted, tenant_id FROM question_bank.question_images WHERE id::text LIKE '0198f370-0014-%' ORDER BY id",
            reader => new ImageRow(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3),
                reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetGuid(5), reader.GetBoolean(6),
                reader.IsDBNull(7) ? null : reader.GetGuid(7)));
    }

    private Task<List<QuestionVersion>> VersionsAsync(Guid questionId) =>
        QueryAsync(context => context.QuestionVersions.IgnoreQueryFilters()
            .Where(version => version.QuestionId == questionId)
            .OrderBy(version => version.Number)
            .ToListAsync());

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

    private sealed record QuestionRow(
        Guid Id,
        int? PaperNumber,
        Guid TopicId,
        Guid? NoticeSubjectId,
        Guid? ExamEditionId,
        Guid? BaseTextId,
        Guid? TenantId,
        string Type,
        string ContentLanguage,
        string StatementHtml,
        string StatementText,
        string NormalizedStatement);

    private sealed record OptionRow(int Position, string TextHtml, bool IsCorrect, Guid? TenantId);

    private sealed record BaseTextRow(Guid Id, string Title, string NormalizedTitle, string ContentLanguage, string TextHtml, Guid? TenantId);

    private sealed record ImageRow(
        Guid Id, string BlobName, string ContentType, long SizeBytes, string? OwnerKind, Guid? OwnerId, bool IsDeleted, Guid? TenantId);
}
