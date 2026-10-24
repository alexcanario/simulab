using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-108 through HTTP, on a real PostgreSQL: authoring a short-answer question, its one blank without a marker, the
/// draft and publish rules, the refusals of BR2 to BR4 and BR7 with position 1, the key change that needs a reason
/// (BR9), the rules the types share, the base-text fan-out, the list, the responses and the permission.
/// </summary>
public sealed class ShortAnswerQuestionEndpointTests : OriginApiTests
{
    private const string BaseTexts = "/api/v1/question-bank/base-texts";
    private const string CapitalStatement = "<p>Qual é a capital do Brasil?</p>";

    private static SaveBlankRequest Blank(int number, params string[] answers) => new(number, answers);

    private static SaveQuestionRequest Short(
        Guid topicId,
        string statement = CapitalStatement,
        IReadOnlyList<SaveBlankRequest>? blanks = null) =>
        new(topicId, "pt-BR", statement, Type: QuestionType.ShortAnswer, Blanks: blanks);

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/publish", null);

    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<IReadOnlyList<QuestionVersionListItemResponse>>(
            $"{Questions}/{id}/versions", AppJson.Options))!;

    private static async Task<QuestionVersionResponse> VersionAsync(HttpClient client, Guid id, int number) =>
        (await client.GetFromJsonAsync<QuestionVersionResponse>($"{Questions}/{id}/versions/{number}", AppJson.Options))!;

    private static async Task<QuestionResponse> PublishedShortAsync(HttpClient admin, Guid topicId, Guid? baseTextId = null)
    {
        var question = await CreatedAsync(admin, Short(topicId, blanks: [Blank(1, "Brasília")]) with { BaseTextId = baseTextId });
        var response = await PublishAsync(admin, question.Id);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return question;
    }

    private static async Task<string> RefusalAsync(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, body);

        return CodeOf(body)!;
    }

    // The code and the position of a row refusal: for the blank codes, the position is the blank number.
    private static async Task<(string Code, int? Position)> RowRefusalAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        var problem = JsonDocument.Parse(body).RootElement;

        return (
            problem.GetProperty("code").GetString()!,
            problem.TryGetProperty("position", out var position) ? position.GetInt32() : null);
    }

    private static IEnumerable<(int Number, IReadOnlyList<string> Answers)> BlanksOf(IReadOnlyList<BlankResponse>? blanks) =>
        (blanks ?? []).Select(blank => (blank.Number, blank.AcceptedAnswers));

    // AC1, BR1, BR3: a draft of the type, listed with it, with its one blank numbered 1 and no answer.
    [Fact]
    public async Task Create_WithNoBlanks_IsStoredAsADraftOfThatType_WithOneEmptyBlank_AndListedWithIt()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, Short(topic.Id));

        question.Type.Should().Be(QuestionType.ShortAnswer);
        question.Status.Should().Be(QuestionStatus.Draft);
        question.Options.Should().BeEmpty();
        BlanksOf(question.Blanks).Should().ContainSingle().Which.Should().Be((1, (IReadOnlyList<string>)[]));
        (await ListAsync(admin, "?pageSize=100")).Items.Single(item => item.Id == question.Id)
            .Type.Should().Be(QuestionType.ShortAnswer);
    }

    // AC2: both answers are stored trimmed, in the order written.
    [Fact]
    public async Task Create_WithBrasiliaAndBrasilia_StoresBothTrimmedInOrder()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, " Brasília ", "Brasilia")]));

        BlanksOf(question.Blanks).Should().ContainSingle().Which.Answers.Should().Equal("Brasília", "Brasilia");
        BlanksOf((await GetAsync(admin, question.Id)).Blanks).Should().ContainSingle().Which.Answers.Should().Equal("Brasília", "Brasilia");
    }

    // AC3, BR4, BR6: the limits; the position is 1; nothing is stored.
    [Fact]
    public async Task Create_BreakingTheLimits_IsRefusedWithPosition1_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        Task<HttpResponseMessage> CreateWithAsync(params string[] answers) =>
            CreateAsync(admin, Short(topic.Id, $"<p>{marker}</p>", [Blank(1, answers)]));

        var six = await RowRefusalAsync(await CreateWithAsync("a", "b", "c", "d", "e", "f"));
        var tooLong = await RowRefusalAsync(await CreateWithAsync(new string('a', QuestionLimits.BlankAnswerMaxLength + 1)));
        var empty = await RowRefusalAsync(await CreateWithAsync("Brasília", "   "));
        var repeated = await RowRefusalAsync(await CreateWithAsync("Brasília", " brasília  "));

        six.Should().Be((QuestionBankErrorCodes.QuestionBlankAnswersCount, 1));
        tooLong.Should().Be((QuestionBankErrorCodes.QuestionBlankAnswerTooLong, 1));
        empty.Should().Be((QuestionBankErrorCodes.QuestionBlankAnswerRequired, 1));
        repeated.Should().Be((QuestionBankErrorCodes.QuestionBlankAnswerRepeated, 1));
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithAnAnswerOf100Characters_IsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var longest = new string('a', QuestionLimits.BlankAnswerMaxLength);

        var question = await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, longest)]));

        BlanksOf(question.Blanks).Should().ContainSingle().Which.Answers.Should().Equal(longest);
    }

    // AC4, BR3: two blanks, or one not numbered 1, are refused; nothing is stored.
    [Fact]
    public async Task Create_WithTwoBlanksOrABlankNumbered2_IsRefused_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();

        var two = await CreateAsync(admin, Short(topic.Id, $"<p>{marker}</p>", [Blank(1, "a"), Blank(2, "b")]));
        var second = await CreateAsync(admin, Short(topic.Id, $"<p>{marker}</p>", [Blank(2, "a")]));

        (await RefusalAsync(two)).Should().Be(QuestionBankErrorCodes.QuestionBlanksCount);
        (await RefusalAsync(second)).Should().Be(QuestionBankErrorCodes.QuestionBlankMarkerMissing);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    // AC5, BR5
    [Fact]
    public async Task Publish_WithNoAnswer_IsRefused_AndWithOne_IsPublished()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var empty = await CreatedAsync(admin, Short(topic.Id));
        var complete = await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, "Brasília")]));

        (await RefusalAsync(await PublishAsync(admin, empty.Id))).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersRequired);
        var response = await PublishAsync(admin, complete.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!.Status.Should().Be(QuestionStatus.Published);
        (await GetAsync(admin, empty.Id)).Status.Should().Be(QuestionStatus.Draft);
    }

    // AC6, BR5: a published question stays complete; a refusal leaves it as it was.
    [Fact]
    public async Task Update_Published_RemovingEveryAnswer_IsRefused_AndTheStoredQuestionIsUnchanged_AndAnotherValidSetStaysPublished()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedShortAsync(admin, topic.Id);

        var none = await UpdateAsync(admin, question.Id, Short(topic.Id, "<p>Outro enunciado</p>", [Blank(1)]));

        (await RowRefusalAsync(none)).Should().Be((QuestionBankErrorCodes.QuestionBlankAnswersRequired, 1));
        var stored = await GetAsync(admin, question.Id);
        stored.StatementHtml.Should().Be(CapitalStatement);
        BlanksOf(stored.Blanks).Should().ContainSingle().Which.Answers.Should().Equal("Brasília");
        var updated = await UpdatedAsync(
            admin, question.Id, Short(topic.Id, blanks: [Blank(1, "Brasília", "Capital Federal")]) with { ChangeReason = "Outro nome." });
        updated.Status.Should().Be(QuestionStatus.Published);
        BlanksOf(updated.Blanks).Should().ContainSingle().Which.Answers.Should().Equal("Brasília", "Capital Federal");
    }

    // AC7, BR7: what the type does not have is refused, never ignored; nothing is stored.
    [Fact]
    public async Task Create_WithOptionsAKeyOrMatchItems_IsRefused_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var request = Short(topic.Id, $"<p>{marker}</p>", [Blank(1, "x")]);

        var options = await CreateAsync(admin, request with { Options = [new SaveQuestionOptionRequest("<p>A</p>")] });
        var key = await CreateAsync(admin, request with { TrueFalseKey = true });
        var items = await CreateAsync(admin, request with { MatchItems = [new SaveMatchItemRequest("<p>Um</p>")] });

        (await RefusalAsync(options)).Should().Be(QuestionBankErrorCodes.QuestionOptionsNotAllowed);
        (await RefusalAsync(key)).Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        (await RefusalAsync(items)).Should().Be(QuestionBankErrorCodes.QuestionMatchItemsNotAllowed);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    // AC8, BR2: the type never changes; naming its own type is accepted.
    [Fact]
    public async Task Update_NamingAnotherType_Is409TypeChanged_AndNamingItsOwnIsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, "Brasília")]));

        var other = await UpdateAsync(admin, question.Id, Complete(topic.Id, "<p>Mudou</p>") with { Type = QuestionType.SingleChoice });
        var own = await UpdatedAsync(admin, question.Id, Short(topic.Id, blanks: [Blank(1, "Brasília", "Brasilia")]));

        (await RefusalAsync(other, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        own.Type.Should().Be(QuestionType.ShortAnswer);
        BlanksOf(own.Blanks).Should().ContainSingle().Which.Answers.Should().Equal("Brasília", "Brasilia");
    }

    // AC9, BR7: [[1]] is plain text of the statement; the excerpt shows it as typed.
    [Fact]
    public async Task Create_WithStatementTextThatLooksLikeAMarker_KeepsItAsTyped_InTheStatementAndTheExcerpt()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();

        var question = await CreatedAsync(admin, Short(topic.Id, $"<p>{marker} digite [[1]] assim</p>", [Blank(1, "x")]));

        question.StatementHtml.Should().Contain("[[1]]");
        (await ListAsync(admin, $"?search={marker}")).Items.Should().ContainSingle()
            .Which.StatementExcerpt.Should().Be($"{marker} digite [[1]] assim");
    }

    // AC10, BR9: a new accepted answer needs a reason and records a key change with the blank.
    [Fact]
    public async Task Update_Published_AddingAnAcceptedAnswer_NeedsAReason_AndThenRecordsAKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedShortAsync(admin, topic.Id);
        var widened = Short(topic.Id, blanks: [Blank(1, "Brasília", "Brasilia")]);

        var refused = await UpdateAsync(admin, question.Id, widened);

        (await RefusalAsync(refused)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        await UpdatedAsync(admin, question.Id, widened with { ChangeReason = "Aceita a grafia sem acento." });

        var version = await VersionAsync(admin, question.Id, 2);
        version.AnswerKeyChanged.Should().BeTrue();
        version.Reason.Should().Be("Aceita a grafia sem acento.");
        BlanksOf(version.Blanks).Should().ContainSingle().Which.Answers.Should().Equal("Brasília", "Brasilia");
        BlanksOf((await VersionAsync(admin, question.Id, 1)).Blanks).Should().ContainSingle().Which.Answers.Should().Equal("Brasília");
    }

    // AC11, BR9: the statement, the order or the case of the answers are not a key change.
    [Fact]
    public async Task Update_Published_ChangingOnlyTheStatement_OrTheOrderAndCaseOfTheAnswers_RecordsAVersionWithNoKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, "Brasília", "Brasilia")]));
        (await PublishAsync(admin, question.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        await UpdatedAsync(admin, question.Id, Short(topic.Id, "<p>Qual é a capital federal?</p>", [Blank(1, "Brasília", "Brasilia")]));
        await UpdatedAsync(admin, question.Id, Short(topic.Id, "<p>Qual é a capital federal?</p>", [Blank(1, "BRASILIA", "brasília")]));

        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(3);
        (await VersionAsync(admin, question.Id, 2)).AnswerKeyChanged.Should().BeFalse();
        (await VersionAsync(admin, question.Id, 3)).AnswerKeyChanged.Should().BeFalse();
    }

    // AC12 (F-106 trap T3): editing the linked base text records a version that keeps the blank.
    [Fact]
    public async Task UpdateBaseText_LinkedToAPublishedShortAnswer_RecordsAVersionThatKeepsTheBlank()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var created = await admin.PostAsJsonAsync(
            BaseTexts, new SaveBaseTextRequest($"Texto {Marker()}", "pt-BR", "<p>Brasília nasceu no cerrado.</p>"), AppJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var text = (await created.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
        var question = await PublishedShortAsync(admin, topic.Id, text.Id);

        var updated = await admin.PutAsJsonAsync(
            $"{BaseTexts}/{text.Id}", new SaveBaseTextRequest(text.Title, "pt-BR", "<p>Brasília nasceu no planalto.</p>"), AppJson.Options);

        updated.StatusCode.Should().Be(HttpStatusCode.OK, await updated.Content.ReadAsStringAsync());
        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(2);
        versions[0].AnswerKeyChanged.Should().BeFalse();
        BlanksOf((await VersionAsync(admin, question.Id, 2)).Blanks).Should().ContainSingle().Which.Number.Should().Be(1);
    }

    // AC12, BR8, BR10: the rules the types share hold for this type.
    [Fact]
    public async Task Create_SanitizesTheStatement()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        const string hostile = "<p onclick=\"x()\">Capital? <sup>2</sup></p><script>alert(1)</script>";

        var question = await CreatedAsync(admin, Short(topic.Id, hostile, [Blank(1, "Brasília")]));

        question.StatementHtml.Should().NotContain("script").And.NotContain("onclick").And.Contain("<sup>2</sup>");
        BlanksOf(question.Blanks).Should().ContainSingle();
    }

    [Fact]
    public async Task Create_WithElevenImagesInTheStatement_IsRefusedWithImagesTooMany()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var images = string.Concat(Enumerable.Range(0, 11).Select(_ => $"<img src=\"/question-images/{Guid.CreateVersion7()}\" alt=\"x\">"));

        var response = await CreateAsync(admin, Short(topic.Id, $"<p>x {images}</p>", [Blank(1, "a")]));

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionImagesTooMany);
        BlanksOf((await CreatedAsync(admin, Short(topic.Id, "<p>x</p>", [Blank(1, "a")]))).Blanks)
            .Should().ContainSingle("the same statement without the pictures is stored with its blank");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Create_WithADifficultyOutOfRange_IsRefused(int difficulty)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(admin, Short(topic.Id, blanks: [Blank(1, "a")]) with { Difficulty = difficulty });

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionDifficultyInvalid);
        (await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, "a")]) with { Difficulty = 3 })).Difficulty.Should().Be(3);
    }

    [Fact]
    public async Task Create_WithAPaperNumberAlreadyTaken_IsRefusedWithPaperNumberTaken()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, "a")]) with { ExamEditionId = edition.Id, PaperNumber = 9 });

        var response = await CreateAsync(admin, Short(topic.Id, blanks: [Blank(1, "a")]) with { ExamEditionId = edition.Id, PaperNumber = 9 });

        (await RefusalAsync(response, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberTaken);
    }

    [Fact]
    public async Task Delete_Published_IsRefused_AndADraftIsDeleted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await PublishedShortAsync(admin, topic.Id);
        var draft = await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, "Brasília")]));

        var refused = await admin.DeleteAsync($"{Questions}/{published.Id}");
        var deleted = await admin.DeleteAsync($"{Questions}/{draft.Id}");

        (await RefusalAsync(refused, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTopic_WithOnlyAShortAnswerQuestion_IsRefusedWithTopicInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, "Brasília")]));

        var response = await admin.DeleteAsync($"/api/v1/catalog/topics/{topic.Id}");

        (await RefusalAsync(response, HttpStatusCode.Conflict)).Should().Be(CatalogErrorCodes.TopicInUse);
    }

    [Fact]
    public async Task AnnulAndReinstate_KeepTheBlankInTheirVersions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedShortAsync(admin, topic.Id);

        var annul = await admin.PostAsJsonAsync($"{Questions}/{question.Id}/annul", new AnnulQuestionRequest("Anulada."), AppJson.Options);
        var reinstate = await admin.PostAsJsonAsync(
            $"{Questions}/{question.Id}/reinstate", new ReinstateQuestionRequest("Recurso indeferido."), AppJson.Options);

        annul.StatusCode.Should().Be(HttpStatusCode.OK, await annul.Content.ReadAsStringAsync());
        reinstate.StatusCode.Should().Be(HttpStatusCode.OK, await reinstate.Content.ReadAsStringAsync());
        BlanksOf((await VersionAsync(admin, question.Id, 2)).Blanks).Should().ContainSingle().Which.Answers.Should().Equal("Brasília");
        BlanksOf((await VersionAsync(admin, question.Id, 3)).Blanks).Should().ContainSingle().Which.Answers.Should().Equal("Brasília");
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Published);
    }

    [Fact]
    public async Task Submit_CompleteDraft_GoesToReview_AndAnIncompleteOneIsRefused()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var complete = await CreatedAsync(admin, Short(topic.Id, blanks: [Blank(1, "Brasília")]));
        var incomplete = await CreatedAsync(admin, Short(topic.Id));

        var sent = await admin.PostAsync($"{Questions}/{complete.Id}/submit", null);
        var refused = await admin.PostAsync($"{Questions}/{incomplete.Id}/submit", null);

        sent.StatusCode.Should().Be(HttpStatusCode.OK, await sent.Content.ReadAsStringAsync());
        (await GetAsync(admin, complete.Id)).Status.Should().Be(QuestionStatus.InReview);
        (await RefusalAsync(refused)).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersRequired);
    }

    // AC13, BR12: every type is named in the list; the responses of the other types carry no blank.
    [Fact]
    public async Task List_QuestionsOfTheSixTypes_NameTheTypeOfEachRow_AndOnlyFillInAndShortAnswerCarryBlanks()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var single = await CreatedAsync(admin, Complete(topic.Id, $"<p>{marker} escolha</p>"));
        var trueFalse = await CreatedAsync(
            admin, new(topic.Id, "pt-BR", $"<p>{marker} certo</p>", Type: QuestionType.TrueFalse, TrueFalseKey: true));
        var multiple = await CreatedAsync(admin, new(topic.Id, "pt-BR", $"<p>{marker} várias</p>", Type: QuestionType.MultipleAnswer));
        var matching = await CreatedAsync(admin, new(topic.Id, "pt-BR", $"<p>{marker} associação</p>", Type: QuestionType.Matching));
        var fillIn = await CreatedAsync(
            admin, new(topic.Id, "pt-BR", $"<p>{marker} capital: [[1]]</p>", Type: QuestionType.FillInTheBlanks, Blanks: [Blank(1, "Brasília")]));
        var shortAnswer = await CreatedAsync(admin, Short(topic.Id, $"<p>{marker} capital?</p>", [Blank(1, "Brasília")]));

        var items = (await ListAsync(admin, $"?search={marker}")).Items;

        items.Should().HaveCount(6);
        items.Single(item => item.Id == single.Id).Type.Should().Be(QuestionType.SingleChoice);
        items.Single(item => item.Id == trueFalse.Id).Type.Should().Be(QuestionType.TrueFalse);
        items.Single(item => item.Id == multiple.Id).Type.Should().Be(QuestionType.MultipleAnswer);
        items.Single(item => item.Id == matching.Id).Type.Should().Be(QuestionType.Matching);
        items.Single(item => item.Id == fillIn.Id).Type.Should().Be(QuestionType.FillInTheBlanks);
        items.Single(item => item.Id == shortAnswer.Id).Type.Should().Be(QuestionType.ShortAnswer);
        foreach (var other in new[] { single, trueFalse, multiple, matching })
        {
            (await GetAsync(admin, other.Id)).Blanks.Should().NotBeNull().And.BeEmpty();
        }

        BlanksOf((await GetAsync(admin, shortAnswer.Id)).Blanks).Should().ContainSingle();
    }

    // AC17, BR10: the same permission as every question endpoint.
    [Fact]
    public async Task CreateAndUpdate_WithoutThePermission_AnswerForbidden()
    {
        var student = await StudentAsync();
        var request = Short(Guid.CreateVersion7(), blanks: [Blank(1, "Brasília")]);

        var create = await student.PostAsJsonAsync(Questions, request, AppJson.Options);
        var update = await student.PutAsJsonAsync($"{Questions}/{Guid.CreateVersion7()}", request, AppJson.Options);

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The same request by a curator, who holds the permission, is stored with its blank.
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        BlanksOf((await CreatedAsync(curator, Short(topic.Id, blanks: [Blank(1, "Brasília")]))).Blanks).Should().ContainSingle();
    }
}
