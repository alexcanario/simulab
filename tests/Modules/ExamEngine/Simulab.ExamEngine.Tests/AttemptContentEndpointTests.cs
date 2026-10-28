using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-169 through HTTP: what the student reads to take an attempt (BR2, BR3) and who may read it (BR11, T1, T2, T6).
/// </summary>
public sealed class AttemptContentEndpointTests : AttemptContentApiTests
{
    // AC2: the version fixed at the start, not the one the curator saved later.
    [Fact]
    public async Task Content_AfterTheQuestionWasEdited_ShowsTheStatementAndOptionsOfTheFixedVersion()
    {
        var admin = await AdminAsync();
        var topicId = await TopicAsync(admin);
        var edition = await EditionAsync(admin, publish: false);
        var question = await PublishedQuestionAsync(admin, topicId, edition.Id, 1, AttemptQuestionType.SingleChoice);
        await PublishEditionAsync(admin, edition);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);

        (await admin.PostAsync($"{Questions}/{question.Id}/unpublish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var edit = await admin.PutAsJsonAsync(
            $"{Questions}/{question.Id}",
            QuestionOfType(topicId, edition.Id, 1, AttemptQuestionType.SingleChoice) with
            {
                StatementHtml = "<p>Mudou</p>",
                Options = [new("<p>Paris</p>", true), new("<p>Lyon</p>"), new("<p>Marselha</p>")],
                ChangeReason = "Ajuste do enunciado."
            },
            AppJson.Options);
        edit.StatusCode.Should().Be(HttpStatusCode.OK, await edit.Content.ReadAsStringAsync());
        (await admin.PostAsync($"{Questions}/{question.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await ReadContentAsync(student, attempt.Id);

        var shown = content.Questions.Should().ContainSingle().Subject;
        shown.VersionNumber.Should().Be(1);
        shown.StatementHtml.Should().Contain("Enunciado 1").And.NotContain("Mudou");
        shown.Options.Select(option => option.Position).Should().Equal(0, 1, 2);
        shown.Options.Select(option => option.TextHtml).Should().SatisfyRespectively(
            first => first.Should().Contain("Paris"),
            second => second.Should().Contain("Lyon"),
            third => third.Should().Contain("Nice"));
        shown.ContentLanguage.Should().Be("pt-BR");
    }

    // AC3: sections are the notice subjects with their labels, ordered by their lowest paper number, each in paper
    // order; a question with no notice subject is in a last section with no id. The edition names the bar.
    [Fact]
    public async Task Content_GroupsTheQuestionsInSectionsByNoticeSubject_InPaperOrder_WithTheOthersLast()
    {
        var admin = await AdminAsync();
        var topicId = await TopicAsync(admin);
        var edition = await EditionAsync(admin, publish: false);
        var legislation = await NoticeSubjectAsync(admin, edition, "Legislação municipal");
        var portuguese = await NoticeSubjectAsync(admin, edition, "Língua portuguesa");
        var ids = new Dictionary<int, Guid>();
        foreach (var (number, subject) in new (int, Guid?)[] { (1, portuguese), (2, portuguese), (3, legislation), (4, legislation), (5, portuguese), (6, null) })
        {
            var question = await PublishedAsync(
                admin, QuestionOfType(topicId, edition.Id, number, AttemptQuestionType.SingleChoice) with { NoticeSubjectId = subject });
            ids[number] = question.Id;
        }

        await PublishEditionAsync(admin, edition);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);

        var content = await ReadContentAsync(student, attempt.Id);

        content.Questions.Select(question => question.PaperNumber).Should().Equal(1, 2, 3, 4, 5, 6);
        content.Sections.Should().HaveCount(3);
        content.Sections[0].Should().BeEquivalentTo(new AttemptSectionResponse(1, 5, [ids[1], ids[2], ids[5]], portuguese, "Língua portuguesa"));
        content.Sections[1].Should().BeEquivalentTo(new AttemptSectionResponse(3, 4, [ids[3], ids[4]], legislation, "Legislação municipal"));
        content.Sections[2].Should().BeEquivalentTo(new AttemptSectionResponse(6, 6, [ids[6]]));
        content.Edition.Should().NotBeNull();
        content.Edition!.ExamId.Should().Be(edition.ExamId);
        content.Edition.NoticeYear.Should().Be(2024);
        content.Edition.ContentLanguage.Should().Be("pt-BR");
    }

    // AC12 (Api): the base text comes with the question, and every picture address points at the attempt.
    [Fact]
    public async Task Content_CarriesTheBaseText_AndPointsEveryPictureAtTheAttempt()
    {
        var admin = await AdminAsync();
        var topicId = await TopicAsync(admin);
        var edition = await EditionAsync(admin, publish: false);
        var inStatement = await UploadedAsync(admin);
        var inBaseText = await UploadedAsync(admin);
        var inOption = await UploadedAsync(admin);
        var baseTextId = await BaseTextAsync(admin, $"<p>Leia o texto.</p>{Img(inBaseText)}");
        await PublishedAsync(admin, QuestionOfType(topicId, edition.Id, 1, AttemptQuestionType.SingleChoice) with
        {
            StatementHtml = $"<p>Observe a figura.</p>{Img(inStatement)}",
            BaseTextId = baseTextId,
            Options = [new($"<p>Esta</p>{Img(inOption)}", true), new("<p>Lyon</p>"), new("<p>Nice</p>")]
        });
        await PublishEditionAsync(admin, edition);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);

        var question = (await ReadContentAsync(student, attempt.Id)).Questions.Single();

        question.BaseTextHtml.Should().Contain("Leia o texto.").And.Contain($"/attempts/{attempt.Id}/images/{inBaseText}");
        question.StatementHtml.Should().Contain($"/attempts/{attempt.Id}/images/{inStatement}");
        question.Options[0].TextHtml.Should().Contain($"/attempts/{attempt.Id}/images/{inOption}");
        Json(question).Should().NotContain("/question-images/");
    }

    // AC11 (Api): the owner still reads a submitted attempt, with its time and answers; the content feeds that page.
    [Fact]
    public async Task Content_OfASubmittedAttempt_IsReadByItsOwner_WithTheTimeAndTheAnswers()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);
        var single = questions.Single(question => question.Type == AttemptQuestionType.SingleChoice);
        await SavedAsync(student, attempt.Id, single.Id, new SingleChoicePayload(2));
        var submitted = await SubmittedAsync(student, attempt.Id);

        var content = await ReadContentAsync(student, attempt.Id);

        content.Status.Should().Be(AttemptStatus.Submitted);
        content.SubmittedAt.Should().Be(submitted.SubmittedAt);
        content.Questions.Should().HaveCount(8);
        content.Questions.Single(question => question.QuestionId == single.Id).Answer.Should().Be(new SingleChoicePayload(2));
        content.Questions.Where(question => question.QuestionId != single.Id).Should().OnlyContain(question => question.Answer == null);
    }

    // AC13: [sec] T1, T6. Another student, an Admin (attempts.read) and an unknown id answer alike; anonymous gets 401.
    [Fact]
    public async Task Content_OfAnotherStudentsAttempt_AnswersLikeAnUnknownId_EvenForAnAdmin_AndAnonymousGets401()
    {
        var admin = await AdminAsync();
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var owner = await StudentAsync();
        var other = await StudentAsync();
        var attempt = await StartedAsync(owner, edition.Id);

        var foreign = await ContentAsync(other, attempt.Id);
        var asAdmin = await ContentAsync(admin, attempt.Id);
        var unknown = await ContentAsync(other, Guid.CreateVersion7());
        var anonymous = await ContentAsync(Client(), attempt.Id);

        foreach (var refused in new[] { foreign, asAdmin, unknown })
        {
            var body = await BodyAsync(refused, HttpStatusCode.NotFound);
            CodeOf(body).Should().Be(ExamAttemptErrorCodes.NotFound);
            ProblemShape(body).Should().Be(ProblemShape(await unknown.Content.ReadAsStringAsync()));
            body.Should().NotContain("Enunciado");
        }

        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadContentAsync(owner, attempt.Id)).Questions.Should().HaveCount(8);
    }

    // AC14: [sec] T2. The content of every type carries no answer key, true/false key, explanation, rubric or accepted answer.
    [Fact]
    public async Task Content_NeverCarriesAnAnswerKeyATrueFalseKeyAnExplanationOrARubric()
    {
        var admin = await AdminAsync();
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);

        var response = await ContentAsync(student, attempt.Id);
        var body = await BodyAsync(response, HttpStatusCode.OK);

        body.Should().Contain("Enunciado 1").And.Contain("Paris");
        body.Should().NotContainAny("SECRET-EXPLANATION", "SECRET-RUBRIC", "Brasília", "1960");
        body.ToUpperInvariant().Should().NotContainAny("ISCORRECT", "EXPLANATION", "RUBRIC", "ACCEPTEDANSWERS", "TRUEFALSEKEY", "MATCHESOPTION", "\"KEY\"");
    }

    // The same members whatever the caller: the shape of a problem body without its trace id.
    private static string ProblemShape(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        return $"{root.GetProperty("status")}|{root.GetProperty("title")}|{root.GetProperty("code")}";
    }
}
