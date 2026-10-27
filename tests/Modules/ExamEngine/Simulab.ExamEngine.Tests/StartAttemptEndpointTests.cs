using System.Net;
using System.Net.Http.Json;
using System.Text;
using Simulab.ExamEngine.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 UC1 through HTTP, on a real PostgreSQL: starting an attempt (BR1 to BR3), the questions it fixes (BR2) and who
/// may start one (T1, T7).
/// </summary>
public sealed class StartAttemptEndpointTests : ExamEngineApiTests
{
    // AC1
    [Fact]
    public async Task Start_PublishedEdition_Answers201WithThePublishedQuestionsInPaperNumberOrder()
    {
        var admin = await AdminAsync();
        var topicId = await TopicAsync(admin);
        var edition = await EditionAsync(admin, publish: false);
        var third = await PublishedQuestionAsync(admin, topicId, edition.Id, 5, AttemptQuestionType.SingleChoice);
        var first = await PublishedQuestionAsync(admin, topicId, edition.Id, 1, AttemptQuestionType.TrueFalse);
        var second = await PublishedQuestionAsync(admin, topicId, edition.Id, 3, AttemptQuestionType.Essay);
        await DraftQuestionAsync(admin, topicId, edition.Id, 2);
        var annulled = await PublishedQuestionAsync(admin, topicId, edition.Id, 4, AttemptQuestionType.SingleChoice);
        (await admin.PostAsJsonAsync($"{Questions}/{annulled.Id}/annul", new Simulab.QuestionBank.Contracts.AnnulQuestionRequest("Anulada pela banca."), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        await PublishEditionAsync(admin, edition);
        var student = await StudentAsync();

        var response = await StartAsync(student, edition.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var attempt = (await response.Content.ReadFromJsonAsync<AttemptResponse>(AppJson.Options))!;
        attempt.Status.Should().Be(AttemptStatus.InProgress);
        attempt.ExamEditionId.Should().Be(edition.Id);
        attempt.StartedAt.Should().Be(Factory.Clock.GetUtcNow());
        attempt.SubmittedAt.Should().BeNull();
        attempt.Questions.Select(question => question.QuestionId).Should().Equal(first.Id, second.Id, third.Id);
        attempt.Questions.Select(question => question.PaperNumber).Should().Equal(1, 3, 5);
        attempt.Questions.Select(question => question.VersionNumber).Should().OnlyContain(version => version == 1);
        attempt.Questions.Select(question => question.Type)
            .Should().Equal(AttemptQuestionType.TrueFalse, AttemptQuestionType.Essay, AttemptQuestionType.SingleChoice);
        attempt.Questions.Should().OnlyContain(question => question.Answer == null);
        response.Headers.Location!.ToString().Should().EndWith($"{Attempts}/{attempt.Id}");
    }

    // AC2: [sec] T7
    [Fact]
    public async Task Start_WhileOneIsInProgress_Answers200WithTheSameAttempt_AndCreatesNoSecondRow()
    {
        var admin = await AdminAsync();
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var (student, studentId) = await SignedInWithIdAsync(IdentityRoles.Student);
        var first = await StartedAsync(student, edition.Id);

        var again = await StartAsync(student, edition.Id);

        again.StatusCode.Should().Be(HttpStatusCode.OK, await again.Content.ReadAsStringAsync());
        (await again.Content.ReadFromJsonAsync<AttemptResponse>(AppJson.Options))!.Id.Should().Be(first.Id);
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempts WHERE student_id = '{studentId}'")).Should().Be(1);
    }

    // AC2: [sec] T7, also when two starts arrive at the same time.
    [Fact]
    public async Task Start_ManyAtTheSameTime_CreatesOneAttemptAndAnswersTheOthersWithIt()
    {
        var admin = await AdminAsync();
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var (student, studentId) = await SignedInWithIdAsync(IdentityRoles.Student);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => StartAsync(student, edition.Id)));

        responses.Select(response => response.StatusCode)
            .Should().OnlyContain(status => status == HttpStatusCode.Created || status == HttpStatusCode.OK);
        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        var ids = new HashSet<Guid>();
        foreach (var response in responses)
        {
            ids.Add((await response.Content.ReadFromJsonAsync<AttemptResponse>(AppJson.Options))!.Id);
        }

        ids.Should().ContainSingle();
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempts WHERE student_id = '{studentId}'")).Should().Be(1);
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempt_answers WHERE attempt_id = '{ids.Single()}'")).Should().Be(8);
    }

    // AC3
    [Fact]
    public async Task Start_AfterTheFirstWasSubmitted_CreatesANewAttempt_AndLeavesTheSubmittedOneAlone()
    {
        var admin = await AdminAsync();
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();
        var first = await StartedAsync(student, edition.Id);
        var submitted = await SubmittedAsync(student, first.Id);

        var second = await StartedAsync(student, edition.Id);

        second.Id.Should().NotBe(first.Id);
        second.Status.Should().Be(AttemptStatus.InProgress);
        var reread = await ReadAsync(student, first.Id);
        reread.Status.Should().Be(AttemptStatus.Submitted);
        reread.SubmittedAt.Should().Be(submitted.SubmittedAt);
    }

    // AC4
    [Fact]
    public async Task Start_OnADraftEditionOrAnUnknownOne_Answers404_AndCreatesNothing()
    {
        var admin = await AdminAsync();
        var topicId = await TopicAsync(admin);
        var draft = await EditionAsync(admin, publish: false);
        await PublishedQuestionAsync(admin, topicId, draft.Id, 1, AttemptQuestionType.SingleChoice);
        var (student, studentId) = await SignedInWithIdAsync(IdentityRoles.Student);

        foreach (var editionId in new[] { draft.Id, Guid.CreateVersion7() })
        {
            var response = await StartAsync(student, editionId);

            CodeOf(await BodyAsync(response, HttpStatusCode.NotFound)).Should().Be(ExamAttemptErrorCodes.EditionNotFound);
        }

        (await CountAsync($"SELECT count(*) FROM exam_engine.attempts WHERE student_id = '{studentId}'")).Should().Be(0);
    }

    // AC5
    [Fact]
    public async Task Start_OnAPublishedEditionWithNoPublishedQuestion_Answers422()
    {
        var admin = await AdminAsync();
        var topicId = await TopicAsync(admin);
        var edition = await EditionAsync(admin, publish: false);
        await DraftQuestionAsync(admin, topicId, edition.Id, 1);
        await PublishEditionAsync(admin, edition);
        var student = await StudentAsync();

        var response = await StartAsync(student, edition.Id);

        CodeOf(await BodyAsync(response, HttpStatusCode.UnprocessableEntity)).Should().Be(ExamAttemptErrorCodes.EditionWithoutQuestions);
    }

    // AC14: [sec] T1. An edition id is not required by the check, only by the body: the permission comes first.
    [Fact]
    public async Task Start_AnAnonymousCallerIs401_ACuratorIs403_AndAStudentIs201()
    {
        var admin = await AdminAsync();
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var curator = await CuratorAsync();
        var student = await StudentAsync();

        (await StartAsync(Client(), edition.Id)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await StartAsync(curator, edition.Id)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await StartAsync(student, edition.Id)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // AC14: [sec] T10. The seeded roles hold what the item says, checked in the grants the start wrote.
    [Fact]
    public async Task TheSeededRoles_StudentHoldsTakeAndNotRead_AndAdminHoldsBoth()
    {
        _ = Factory.Services;

        (await RolesHoldingAsync(ExamEnginePermissions.Take)).Should().BeEquivalentTo([IdentityRoles.Student, IdentityRoles.Admin]);
        (await RolesHoldingAsync(ExamEnginePermissions.Read)).Should().BeEquivalentTo([IdentityRoles.Admin]);
    }

    private async Task<List<string?>> RolesHoldingAsync(string permission) => await ValuesAsync(
        $"SELECT r.name FROM identity.role_permissions p JOIN identity.roles r ON r.id = p.role_id WHERE p.permission_name = '{permission}'");

    // AC19: with no tenant in the host, which is v1, the rows are global.
    [Fact]
    public async Task Start_InV1_TheRowsOfTheAttemptAreGlobal()
    {
        var admin = await AdminAsync();
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();

        var attempt = await StartedAsync(student, edition.Id);

        (await CountAsync($"SELECT count(*) FROM exam_engine.attempts WHERE id = '{attempt.Id}' AND tenant_id IS NULL")).Should().Be(1);
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempt_answers WHERE attempt_id = '{attempt.Id}' AND tenant_id IS NOT NULL")).Should().Be(0);
    }

    // AC14: a body with no edition is a 400, not a 500. B-35 AC1: with a code of its own and one field entry.
    [Fact]
    public async Task Start_WithNoEdition_Answers400WithEditionRequired()
    {
        var (student, studentId) = await SignedInWithIdAsync(IdentityRoles.Student);

        var response = await student.PostAsync(Attempts, new StringContent("{}", Encoding.UTF8, "application/json"));

        await ShouldBeEditionRequiredAsync(response, studentId);
    }

    // B-35 AC2: the empty Guid is a missing edition, not an unknown one.
    [Fact]
    public async Task Start_WithEmptyEditionId_Answers400WithEditionRequired()
    {
        var (student, studentId) = await SignedInWithIdAsync(IdentityRoles.Student);

        var response = await student.PostAsJsonAsync(Attempts, new StartAttemptRequest(Guid.Empty), AppJson.Options);

        await ShouldBeEditionRequiredAsync(response, studentId);
    }

    private async Task ShouldBeEditionRequiredAsync(HttpResponseMessage response, Guid studentId)
    {
        var body = await BodyAsync(response, HttpStatusCode.BadRequest);
        CodeOf(body).Should().Be(ExamAttemptErrorCodes.EditionRequired);
        ErrorsOf(body).Should().Equal(("examEditionId", ExamAttemptErrorCodes.EditionRequired));
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempts WHERE student_id = '{studentId}'")).Should().Be(0);
    }
}
