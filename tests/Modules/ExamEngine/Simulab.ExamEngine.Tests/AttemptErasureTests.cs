using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.ExamEngine.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Jobs;
using Simulab.Jobs.Persistence;
using Simulab.SharedKernel.Messaging;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing.ApiHost;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-167 UC1 and UC3 through HTTP and the job queue: a student erases the account, the <c>account.erased</c> job runs,
/// and the free texts of every attempt of that student are erased while everything else stays (BR1-BR5).
/// </summary>
public sealed class AttemptErasureTests : ExamEngineApiTests
{
    private const string Erasures = "/api/v1/identity/account-erasures";
    private const string AccountErased = "account.erased";

    private static readonly AttemptQuestionType[] TextTypes =
        [AttemptQuestionType.ShortAnswer, AttemptQuestionType.OpenAnswer, AttemptQuestionType.Essay, AttemptQuestionType.FillInTheBlanks];

    /// <summary>An answer of each type; the four text shapes carry a marker no other row holds.</summary>
    private static AnswerPayload AnswerOf(AttemptQuestionType type) => type switch
    {
        AttemptQuestionType.SingleChoice => new SingleChoicePayload(2),
        AttemptQuestionType.TrueFalse => new TrueFalsePayload(false),
        AttemptQuestionType.MultipleAnswer => new MultipleAnswerPayload([0, 1]),
        AttemptQuestionType.Matching => new MatchingPayload([new(0, 2), new(1, 0)]),
        AttemptQuestionType.FillInTheBlanks => new FillInTheBlanksPayload([new(1, "BLANK-TEXT-ONE"), new(2, "BLANK-TEXT-TWO")]),
        AttemptQuestionType.ShortAnswer => new ShortAnswerPayload("SHORT-TEXT"),
        AttemptQuestionType.OpenAnswer => new OpenAnswerPayload("OPEN-TEXT"),
        AttemptQuestionType.Essay => new EssayPayload("ESSAY-TEXT-ONE\nESSAY-TEXT-TWO"),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    /// <summary>The answer of the type once its texts are erased: the same shape, the blank numbers kept, no text.</summary>
    private static AnswerPayload ErasedOf(AttemptQuestionType type) => type switch
    {
        AttemptQuestionType.FillInTheBlanks => new FillInTheBlanksPayload([new(1, null), new(2, null)]),
        AttemptQuestionType.ShortAnswer => new ShortAnswerPayload(),
        AttemptQuestionType.OpenAnswer => new OpenAnswerPayload(),
        AttemptQuestionType.Essay => new EssayPayload(),
        _ => AnswerOf(type)
    };

    private sealed record Student(HttpClient Client, Guid Id);

    private async Task<Student> NewStudentAsync()
    {
        var (client, id) = await SignedInWithIdAsync(IdentityRoles.Student);
        return new Student(client, id);
    }

    /// <summary>Starts an attempt, answers every question of the paper and submits it when asked.</summary>
    private static async Task<Guid> AnsweredAsync(Student student, CatalogEdition edition, IReadOnlyList<PaperQuestion> questions, bool submit)
    {
        var attempt = await StartedAsync(student.Client, edition.Id);
        foreach (var question in questions)
        {
            await SavedAsync(student.Client, attempt.Id, question.Id, AnswerOf(question.Type));
        }

        if (submit)
        {
            await SubmittedAsync(student.Client, attempt.Id);
        }

        return attempt.Id;
    }

    /// <summary>The account is erased through its own route, then the job queue runs as the worker would.</summary>
    private async Task ErasedAsync(Student student)
    {
        var response = await student.Client.PostAsJsonAsync(Erasures, new EraseAccountRequest(TestAccounts.ValidPassword), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        await Factory.RunJobsAsync();
    }

    /// <summary>Every column of the attempt rows of the student, as the database holds them.</summary>
    private Task<List<string?>> AttemptRowsAsync(Guid studentId) =>
        ValuesAsync($"SELECT row_to_json(a)::text FROM exam_engine.attempts a WHERE student_id = '{studentId}' ORDER BY started_at, id");

    /// <summary>Every column of the answer rows of the student's attempts, by attempt then paper number.</summary>
    private async Task<List<JsonElement>> AnswerRowsAsync(Guid studentId) =>
        [.. (await ValuesAsync(
                $"""
                SELECT row_to_json(r)::text FROM exam_engine.attempt_answers r JOIN exam_engine.attempts a ON a.id = r.attempt_id
                WHERE a.student_id = '{studentId}' ORDER BY a.started_at, a.id, r.paper_number
                """))
            .Select(row => JsonDocument.Parse(row!).RootElement.Clone())];

    private static AnswerPayload? PayloadOf(JsonElement row) =>
        row.GetProperty("answer").ValueKind == JsonValueKind.Null
            ? null
            : row.GetProperty("answer").Deserialize<AnswerPayload>(AppJson.Options);

    private static AttemptQuestionType TypeOf(JsonElement row) => Enum.Parse<AttemptQuestionType>(row.GetProperty("question_type").GetString()!);

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>The erasure job, or null once it succeeded: the runner removes a job that ran.</summary>
    private async Task<Job?> ErasureJobAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        return await context.Jobs.AsNoTracking().SingleOrDefaultAsync(job => job.Type == AccountErased);
    }

    // AC7: the four texts go, every other value stays; the submitted attempt and the one in progress alike.
    [Fact]
    public async Task Erase_ClearsTheFourTextsOfEveryAttempt_AndKeepsEverythingElse()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await NewStudentAsync();
        await AnsweredAsync(student, edition, questions, submit: true);
        await AnsweredAsync(student, edition, questions, submit: false);
        var attemptsBefore = await AttemptRowsAsync(student.Id);
        var answersBefore = await AnswerRowsAsync(student.Id);
        answersBefore.Should().HaveCount(16);

        await ErasedAsync(student);

        (await AttemptRowsAsync(student.Id)).Should().Equal(attemptsBefore, "student id, edition, status, times and audit fields stay");
        (await ValuesAsync($"SELECT status FROM exam_engine.attempts WHERE student_id = '{student.Id}' ORDER BY started_at, id"))
            .Should().Equal("Submitted", "InProgress");
        var answersAfter = await AnswerRowsAsync(student.Id);
        answersAfter.Should().HaveCount(16);
        foreach (var (before, after) in answersBefore.Zip(answersAfter))
        {
            var type = TypeOf(before);
            foreach (var column in new[] { "id", "attempt_id", "question_id", "paper_number", "version_number", "question_type", "created_at", "created_by", "tenant_id" })
            {
                after.GetProperty(column).ToString().Should().Be(before.GetProperty(column).ToString(), $"{column} of the {type} row stays");
            }

            Json(PayloadOf(after)).Should().Be(Json(ErasedOf(type)), $"the {type} answer");
            if (TextTypes.Contains(type))
            {
                after.GetProperty("updated_at").ValueKind.Should().NotBe(JsonValueKind.Null, $"the erased {type} row carries the erasure's stamp");
            }
            else
            {
                after.ToString().Should().Be(before.ToString(), $"the {type} row has nothing to erase and is not written");
            }
        }

        string.Join("|", answersAfter.Select(row => row.ToString())).Should().NotContainAny("SHORT-TEXT", "OPEN-TEXT", "ESSAY-TEXT", "BLANK-TEXT");
    }

    // AC8: [sec] T4. Another student's attempts are not touched, not even their audit fields.
    [Fact]
    public async Task Erase_LeavesEveryRowOfAnotherStudentAsItWas()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var erased = await NewStudentAsync();
        var other = await NewStudentAsync();
        await AnsweredAsync(erased, edition, questions, submit: true);
        await AnsweredAsync(other, edition, questions, submit: true);
        await AnsweredAsync(other, edition, questions, submit: false);
        var attemptsBefore = await AttemptRowsAsync(other.Id);
        var answersBefore = (await AnswerRowsAsync(other.Id)).Select(row => row.ToString()).ToList();

        await ErasedAsync(erased);

        string.Join("|", (await AnswerRowsAsync(erased.Id)).Select(row => row.ToString()))
            .Should().NotContain("SHORT-TEXT", "the erasure ran for the erased student");
        (await AttemptRowsAsync(other.Id)).Should().Equal(attemptsBefore);
        (await AnswerRowsAsync(other.Id)).Select(row => row.ToString()).Should().Equal(answersBefore);
        string.Join("|", answersBefore).Should().Contain("SHORT-TEXT", "the other student's texts are still there");
    }

    // AC9: the event again changes nothing and succeeds; an erased student with no attempt succeeds too.
    [Fact]
    public async Task UserErased_DeliveredAgain_ChangesNothing_AndAStudentWithNoAttemptSucceeds()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await NewStudentAsync();
        await AnsweredAsync(student, edition, questions, submit: true);
        await ErasedAsync(student);
        var answersAfterFirst = (await AnswerRowsAsync(student.Id)).Select(row => row.ToString()).ToList();
        (await ErasureJobAsync()).Should().BeNull("the job ran and was removed");
        Factory.Clock.Advance(TimeSpan.FromMinutes(5));

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            await publisher.PublishAsync(new UserErased(student.Id, Factory.Clock.GetUtcNow()));
        }

        (await AnswerRowsAsync(student.Id)).Select(row => row.ToString()).Should().Equal(answersAfterFirst, "not even the update stamp moves");

        var withoutAttempts = await NewStudentAsync();
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
            var deliver = () => publisher.PublishAsync(new UserErased(withoutAttempts.Id, Factory.Clock.GetUtcNow()));
            await deliver.Should().NotThrowAsync();
        }
    }

    // AC10: [sec] T5. A failure while the second attempt is written leaves all three as they were; the retry erases all.
    [Fact]
    public async Task Erase_FailingHalfWay_ChangesNoAttempt_AndTheRetryErasesAllThree()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await NewStudentAsync();
        await AnsweredAsync(student, edition, questions, submit: true);
        var second = await AnsweredAsync(student, edition, questions, submit: true);
        await AnsweredAsync(student, edition, questions, submit: false);
        var answersBefore = (await AnswerRowsAsync(student.Id)).Select(row => row.ToString()).ToList();
        await ExecuteAsync(
            $"""
            CREATE FUNCTION exam_engine.f167_planted_failure() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'planted failure'; END $$;
            CREATE TRIGGER f167_planted_failure BEFORE UPDATE ON exam_engine.attempt_answers
                FOR EACH ROW WHEN (OLD.attempt_id = '{second}') EXECUTE FUNCTION exam_engine.f167_planted_failure();
            """);

        await ErasedAsync(student);

        (await AnswerRowsAsync(student.Id)).Select(row => row.ToString()).Should().Equal(answersBefore, "none of the three changed");
        var job = (await ErasureJobAsync())!;
        job.Should().NotBeNull("the attempt failed, so the job is still there");
        job.Status.Should().Be(JobStatus.Pending);
        job.Attempts.Should().Be(1);
        job.RunAfter.Should().BeAfter(Factory.Clock.GetUtcNow(), "the job is due again after the backoff");

        await ExecuteAsync("DROP TRIGGER f167_planted_failure ON exam_engine.attempt_answers; DROP FUNCTION exam_engine.f167_planted_failure();");
        Factory.Clock.Advance(job.RunAfter - Factory.Clock.GetUtcNow() + TimeSpan.FromSeconds(1));
        await Factory.RunJobsAsync();

        (await ErasureJobAsync()).Should().BeNull("the retry succeeded");
        var answersAfter = await AnswerRowsAsync(student.Id);
        answersAfter.Should().HaveCount(24);
        string.Join("|", answersAfter.Select(row => row.ToString())).Should().NotContainAny("SHORT-TEXT", "OPEN-TEXT", "ESSAY-TEXT", "BLANK-TEXT");
    }

    // AC11: an Admin holding attempts.read sees the same questions and versions, the choices as saved and no text.
    [Fact]
    public async Task Read_ByAnAdmin_AfterTheErasure_ShowsTheQuestionsTheChoicesAndNoText()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await NewStudentAsync();
        var attemptId = await AnsweredAsync(student, edition, questions, submit: true);
        var before = await ReadAsync(admin, attemptId);

        await ErasedAsync(student);

        var after = await ReadAsync(admin, attemptId);
        after.Questions.Select(question => (question.QuestionId, question.PaperNumber, question.VersionNumber, question.Type))
            .Should().Equal(before.Questions.Select(question => (question.QuestionId, question.PaperNumber, question.VersionNumber, question.Type)));
        foreach (var question in after.Questions)
        {
            Json(question.Answer).Should().Be(Json(ErasedOf(question.Type)), $"the {question.Type} answer");
        }
    }
}
