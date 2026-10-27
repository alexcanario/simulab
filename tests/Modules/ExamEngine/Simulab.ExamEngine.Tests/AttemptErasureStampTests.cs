using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulab.ExamEngine.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing.ApiHost;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-167 AC7, change note v2 (review finding 1): an answer row whose text is erased carries the erasure's update stamp, a
/// new <c>updated_at</c> from the clock of the job and a null <c>updated_by</c> (a system action); a row with nothing to
/// erase keeps the stamp its last save left. A file of its own: the tests of AC7 were locked before the finding.
/// </summary>
public sealed class AttemptErasureStampTests : ExamEngineApiTests
{
    private static AnswerPayload AnswerOf(AttemptQuestionType type) => type switch
    {
        AttemptQuestionType.SingleChoice => new SingleChoicePayload(2),
        AttemptQuestionType.TrueFalse => new TrueFalsePayload(false),
        AttemptQuestionType.MultipleAnswer => new MultipleAnswerPayload([0, 1]),
        AttemptQuestionType.Matching => new MatchingPayload([new(0, 2), new(1, 0)]),
        AttemptQuestionType.FillInTheBlanks => new FillInTheBlanksPayload([new(1, "STAMP-ONE"), new(2, "STAMP-TWO")]),
        AttemptQuestionType.ShortAnswer => new ShortAnswerPayload("STAMP-SHORT"),
        AttemptQuestionType.OpenAnswer => new OpenAnswerPayload("STAMP-OPEN"),
        AttemptQuestionType.Essay => new EssayPayload("STAMP-ESSAY-ONE\nSTAMP-ESSAY-TWO"),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static readonly AttemptQuestionType[] TextTypes =
        [AttemptQuestionType.ShortAnswer, AttemptQuestionType.OpenAnswer, AttemptQuestionType.Essay, AttemptQuestionType.FillInTheBlanks];

    private async Task<Dictionary<string, (string? UpdatedAt, string? UpdatedBy)>> StampsAsync(Guid attemptId)
    {
        var rows = await ValuesAsync(
            $"SELECT json_build_object('type', question_type, 'at', updated_at, 'by', updated_by)::text FROM exam_engine.attempt_answers WHERE attempt_id = '{attemptId}'");

        return rows.Select(row => JsonDocument.Parse(row!).RootElement).ToDictionary(
            row => row.GetProperty("type").GetString()!,
            row => (row.GetProperty("at").ValueKind == JsonValueKind.Null ? null : row.GetProperty("at").GetRawText(),
                row.GetProperty("by").ValueKind == JsonValueKind.Null ? null : row.GetProperty("by").GetString()));
    }

    [Fact]
    public async Task Erase_StampsTheErasedRowsWithANewTimeAndNoUser_AndLeavesTheOthersStamps()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var (student, studentId) = await SignedInWithIdAsync(IdentityRoles.Student);
        var attempt = await StartedAsync(student, edition.Id);
        foreach (var question in questions)
        {
            await SavedAsync(student, attempt.Id, question.Id, AnswerOf(question.Type));
        }

        var before = await StampsAsync(attempt.Id);
        before.Values.Should().OnlyContain(stamp => stamp.UpdatedBy == studentId.ToString(), "each save stamped its row with the student");
        // Less than the 15 minutes of an access token: the student still erases with the same token.
        Factory.Clock.Advance(TimeSpan.FromMinutes(5));

        var erased = await student.PostAsJsonAsync(
            "/api/v1/identity/account-erasures", new EraseAccountRequest(TestAccounts.ValidPassword), AppJson.Options);
        erased.StatusCode.Should().Be(HttpStatusCode.NoContent, await erased.Content.ReadAsStringAsync());
        await Factory.RunJobsAsync();

        var after = await StampsAsync(attempt.Id);
        foreach (var type in Enum.GetValues<AttemptQuestionType>())
        {
            var name = type.ToString();
            if (TextTypes.Contains(type))
            {
                after[name].UpdatedAt.Should().NotBe(before[name].UpdatedAt, $"the erased {name} row has a new update time");
                after[name].UpdatedBy.Should().BeNull($"the erasure of the {name} row is a system action");
            }
            else
            {
                after[name].Should().Be(before[name], $"the {name} row had nothing to erase and keeps its stamp");
            }
        }
    }
}
