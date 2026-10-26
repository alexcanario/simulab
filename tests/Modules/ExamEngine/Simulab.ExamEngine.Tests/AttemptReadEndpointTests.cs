using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulab.ExamEngine.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 UC4 and UC5 through HTTP: what an attempt shows (BR2, BR8) and who reads it (BR7, T2, T4).
/// </summary>
public sealed class AttemptReadEndpointTests : ExamEngineApiTests
{
    // AC6
    [Fact]
    public async Task Read_AfterTheQuestionsWereEditedAnnulledOrUnpublished_StillListsTheSameQuestionsAndVersions()
    {
        var admin = await AdminAsync();
        var topicId = await TopicAsync(admin);
        var edition = await EditionAsync(admin, publish: false);
        var edited = await PublishedQuestionAsync(admin, topicId, edition.Id, 1, AttemptQuestionType.SingleChoice);
        var annulled = await PublishedQuestionAsync(admin, topicId, edition.Id, 2, AttemptQuestionType.SingleChoice);
        var unpublished = await PublishedQuestionAsync(admin, topicId, edition.Id, 3, AttemptQuestionType.SingleChoice);
        await PublishEditionAsync(admin, edition);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);

        (await admin.PostAsync($"{Questions}/{edited.Id}/unpublish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var edit = await admin.PutAsJsonAsync(
            $"{Questions}/{edited.Id}",
            QuestionOfType(topicId, edition.Id, 1, AttemptQuestionType.SingleChoice) with { StatementHtml = "<p>Mudou</p>", ChangeReason = "Ajuste do enunciado." },
            AppJson.Options);
        edit.StatusCode.Should().Be(HttpStatusCode.OK, await edit.Content.ReadAsStringAsync());
        var republished = await admin.PostAsync($"{Questions}/{edited.Id}/publish", null);
        republished.StatusCode.Should().Be(HttpStatusCode.OK, await republished.Content.ReadAsStringAsync());
        (await admin.GetFromJsonAsync<QuestionResponse>($"{Questions}/{edited.Id}", AppJson.Options))!.CurrentVersion
            .Should().BeGreaterThan(1, "the question moved on, the attempt must not");
        (await admin.PostAsJsonAsync($"{Questions}/{annulled.Id}/annul", new AnnulQuestionRequest("Anulada pela banca."), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PostAsync($"{Questions}/{unpublished.Id}/unpublish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var reread = await ReadAsync(student, attempt.Id);

        reread.Questions.Select(question => question.QuestionId).Should().Equal(edited.Id, annulled.Id, unpublished.Id);
        reread.Questions.Select(question => question.VersionNumber).Should().OnlyContain(version => version == 1);
    }

    // AC12
    [Fact]
    public async Task Read_TheOwnerSeesStateQuestionsAndAnswers_AndAnAdminHoldingReadSeesTheSame()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);
        var single = questions.Single(question => question.Type == AttemptQuestionType.SingleChoice);
        await SavedAsync(student, attempt.Id, single.Id, new SingleChoicePayload(1));

        var own = await ReadAsync(student, attempt.Id);
        var asAdmin = await ReadAsync(admin, attempt.Id);

        own.Status.Should().Be(AttemptStatus.InProgress);
        own.Questions.Should().HaveCount(8);
        own.Questions.Single(question => question.QuestionId == single.Id).Answer.Should().Be(new SingleChoicePayload(1));
        own.Questions.Where(question => question.QuestionId != single.Id).Should().OnlyContain(question => question.Answer == null);
        Json(asAdmin).Should().Be(Json(own));
    }

    // AC15: [sec] T2. Another student's attempt and an id that does not exist answer exactly alike.
    [Fact]
    public async Task Read_AnotherStudentsAttempt_AnswersLikeAnIdThatDoesNotExist()
    {
        var admin = await AdminAsync();
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var owner = await StudentAsync();
        var other = await StudentAsync();
        var attempt = await StartedAsync(owner, edition.Id);

        var foreign = await other.GetAsync($"{Attempts}/{attempt.Id}");
        var unknown = await other.GetAsync($"{Attempts}/{Guid.CreateVersion7()}");

        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var foreignBody = await foreign.Content.ReadAsStringAsync();
        CodeOf(foreignBody).Should().Be(ExamAttemptErrorCodes.NotFound);
        ProblemShape(foreignBody).Should().Be(ProblemShape(await unknown.Content.ReadAsStringAsync()));
    }

    // AC14: an anonymous caller reads nothing.
    [Fact]
    public async Task Read_Anonymous_Answers401()
    {
        (await Client().GetAsync($"{Attempts}/{Guid.CreateVersion7()}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC16: [sec] T4. The student's and the admin's responses carry no key, explanation, model answer or statement.
    [Fact]
    public async Task Read_NeverCarriesAnAnswerKeyAnExplanationAModelAnswerOrAStatement()
    {
        var admin = await AdminAsync();
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);

        foreach (var reader in new[] { student, admin })
        {
            var body = await (await reader.GetAsync($"{Attempts}/{attempt.Id}")).Content.ReadAsStringAsync();

            body.Should().NotContainAny("SECRET-EXPLANATION", "SECRET-RUBRIC", "Enunciado", "Paris", "Brasília");
            body.ToUpperInvariant().Should().NotContainAny("ISCORRECT", "EXPLANATION", "RUBRIC", "ACCEPTEDANSWERS", "TRUEFALSEKEY", "STATEMENT");
        }

        var started = await StartAsync(student, edition.Id);
        (await started.Content.ReadAsStringAsync()).Should().NotContainAny("SECRET-EXPLANATION", "SECRET-RUBRIC", "Enunciado");
    }

    // The same four members whatever the caller: the shape of a problem body without its trace id.
    private static string ProblemShape(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        return $"{root.GetProperty("status")}|{root.GetProperty("title")}|{root.GetProperty("code")}";
    }
}
