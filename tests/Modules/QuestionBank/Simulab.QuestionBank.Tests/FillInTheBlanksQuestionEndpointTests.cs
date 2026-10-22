using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-107 through HTTP, on a real PostgreSQL: authoring a fill-in-the-blanks question, the draft and publish rules over
/// its markers and blanks, the refusals of BR2, BR4 and BR7 with the blank number on row refusals, the key change that
/// needs a reason (BR10), the rules the types share, the base-text fan-out, the list excerpt, the responses and the
/// permission.
/// </summary>
public sealed class FillInTheBlanksQuestionEndpointTests : OriginApiTests
{
    private const string BaseTexts = "/api/v1/question-bank/base-texts";
    private const string CapitalStatement = "<p>A capital do Brasil é [[1]] e foi fundada em [[2]].</p>";

    private static SaveBlankRequest Blank(int number, params string[] answers) => new(number, answers);

    private static IReadOnlyList<SaveBlankRequest> CapitalBlanks() => [Blank(1, "Brasília"), Blank(2, "1960")];

    private static SaveQuestionRequest FillIn(
        Guid topicId,
        string statement = CapitalStatement,
        IReadOnlyList<SaveBlankRequest>? blanks = null) =>
        new(topicId, "pt-BR", statement, Type: QuestionType.FillInTheBlanks, Blanks: blanks);

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/publish", null);

    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<IReadOnlyList<QuestionVersionListItemResponse>>(
            $"{Questions}/{id}/versions", AppJson.Options))!;

    private static async Task<QuestionVersionResponse> VersionAsync(HttpClient client, Guid id, int number) =>
        (await client.GetFromJsonAsync<QuestionVersionResponse>($"{Questions}/{id}/versions/{number}", AppJson.Options))!;

    private static async Task<QuestionResponse> PublishedFillInAsync(HttpClient admin, Guid topicId, Guid? baseTextId = null)
    {
        var question = await CreatedAsync(admin, FillIn(topicId, blanks: CapitalBlanks()) with { BaseTextId = baseTextId });
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

    // AC1: type chosen at creation, no marker yet: a draft of that type, listed with its type.
    [Fact]
    public async Task Create_WithNoMarker_IsStoredAsADraftOfThatType_AndListedWithIt()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, FillIn(topic.Id, "<p>Complete a frase.</p>"));

        question.Type.Should().Be(QuestionType.FillInTheBlanks);
        question.Status.Should().Be(QuestionStatus.Draft);
        question.Options.Should().BeEmpty();
        question.Blanks.Should().NotBeNull().And.BeEmpty();
        (await ListAsync(admin, "?pageSize=100")).Items.Single(item => item.Id == question.Id)
            .Type.Should().Be(QuestionType.FillInTheBlanks);
    }

    // AC2, BR3, BR4: markers 1 and 3 with answers for 3 only come back as two blanks in statement order.
    [Fact]
    public async Task Create_DraftWithMarkers1And3_AndAnswersForBlank3Only_ReturnsBothBlanksInStatementOrder()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(
            admin, FillIn(topic.Id, "<p>[[3]] chegou antes de [[1]].</p>", [Blank(3, " Cabral ")]));

        var blanks = BlanksOf(question.Blanks).ToList();
        blanks.Select(blank => blank.Number).Should().Equal(3, 1);
        blanks[0].Answers.Should().Equal("Cabral");
        blanks[1].Answers.Should().BeEmpty();
        BlanksOf((await GetAsync(admin, question.Id)).Blanks).Should().BeEquivalentTo(blanks, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Create_WithElevenMarkers_IsRefusedWithBlanksCount()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var statement = $"<p>{string.Join(' ', Enumerable.Range(1, 11).Select(BlankMarker.For))}</p>";

        var response = await CreateAsync(admin, FillIn(topic.Id, statement));

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionBlanksCount);
    }

    // AC3, BR4: broken data is refused even on a draft, and nothing is stored.
    [Fact]
    public async Task Create_WithARepeatedMarkerOrABlankWithNoMarker_IsRefused_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();

        var repeated = await CreateAsync(admin, FillIn(topic.Id, $"<p>{marker} [[2]] e [[2]]</p>"));
        var missing = await CreateAsync(admin, FillIn(topic.Id, $"<p>{marker} [[1]]</p>", [Blank(1, "a"), Blank(4, "b")]));

        (await RefusalAsync(repeated)).Should().Be(QuestionBankErrorCodes.QuestionBlankRepeated);
        (await RefusalAsync(missing)).Should().Be(QuestionBankErrorCodes.QuestionBlankMarkerMissing);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    // AC4, BR4, BR6: the limits of each blank; the refusal names the blank.
    [Fact]
    public async Task Create_BreakingTheLimitsOfABlank_IsRefusedWithTheBlankNumberAsPosition()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        Task<HttpResponseMessage> CreateWithBlank2Async(params string[] answers) =>
            CreateAsync(admin, FillIn(topic.Id, blanks: [Blank(1, "Brasília"), Blank(2, answers)]));

        var six = await RowRefusalAsync(await CreateWithBlank2Async("a", "b", "c", "d", "e", "f"));
        var tooLong = await RowRefusalAsync(await CreateWithBlank2Async(new string('a', QuestionLimits.BlankAnswerMaxLength + 1)));
        var empty = await RowRefusalAsync(await CreateWithBlank2Async("1960", "   "));
        var repeated = await RowRefusalAsync(await CreateWithBlank2Async("Brasília", " brasília  "));

        six.Should().Be((QuestionBankErrorCodes.QuestionBlankAnswersCount, 2));
        tooLong.Should().Be((QuestionBankErrorCodes.QuestionBlankAnswerTooLong, 2));
        empty.Should().Be((QuestionBankErrorCodes.QuestionBlankAnswerRequired, 2));
        repeated.Should().Be((QuestionBankErrorCodes.QuestionBlankAnswerRepeated, 2));
    }

    [Fact]
    public async Task Create_WithAnAnswerOf100Characters_AndTheSameAnswerWithAndWithoutAccent_IsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var longest = new string('a', QuestionLimits.BlankAnswerMaxLength);

        var question = await CreatedAsync(admin, FillIn(topic.Id, blanks: [Blank(1, "Brasília", "Brasilia"), Blank(2, longest)]));

        BlanksOf(question.Blanks).Select(blank => blank.Answers).Should().SatisfyRespectively(
            first => first.Should().Equal("Brasília", "Brasilia"),
            second => second.Should().Equal(longest));
    }

    // AC5, BR5: one to ten markers, every one answered.
    [Fact]
    public async Task Publish_WithNoMarkerOrAnUnansweredMarker_IsRefused_AndWithEveryMarkerAnswered_IsPublished()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var noMarker = await CreatedAsync(admin, FillIn(topic.Id, "<p>Sem lacuna.</p>"));
        var unanswered = await CreatedAsync(admin, FillIn(topic.Id, blanks: [Blank(1, "Brasília")]));
        var complete = await CreatedAsync(admin, FillIn(topic.Id, blanks: CapitalBlanks()));

        (await RefusalAsync(await PublishAsync(admin, noMarker.Id))).Should().Be(QuestionBankErrorCodes.QuestionBlanksCount);
        (await RefusalAsync(await PublishAsync(admin, unanswered.Id))).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersRequired);
        var response = await PublishAsync(admin, complete.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!.Status.Should().Be(QuestionStatus.Published);
        (await GetAsync(admin, unanswered.Id)).Status.Should().Be(QuestionStatus.Draft);
    }

    // AC6, BR5: a published question stays complete; a refusal leaves it as it was.
    [Fact]
    public async Task Update_Published_RemovingTheAnswersOfABlankOrEveryMarker_IsRefused_AndTheStoredQuestionIsUnchanged()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedFillInAsync(admin, topic.Id);

        var noAnswers = await UpdateAsync(
            admin, question.Id, FillIn(topic.Id, "<p>Outro [[1]] e [[2]].</p>", [Blank(1, "Brasília"), Blank(2)]));
        var noMarkers = await UpdateAsync(admin, question.Id, FillIn(topic.Id, "<p>Sem lacunas.</p>"));

        (await RowRefusalAsync(noAnswers)).Should().Be((QuestionBankErrorCodes.QuestionBlankAnswersRequired, 2));
        (await RefusalAsync(noMarkers)).Should().Be(QuestionBankErrorCodes.QuestionBlanksCount);
        var stored = await GetAsync(admin, question.Id);
        stored.Status.Should().Be(QuestionStatus.Published);
        stored.StatementHtml.Should().Be(CapitalStatement);
        BlanksOf(stored.Blanks).Select(blank => blank.Answers.Single()).Should().Equal("Brasília", "1960");
    }

    [Fact]
    public async Task Update_Published_WithAnotherValidSetAndAReason_StaysPublished()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedFillInAsync(admin, topic.Id);

        var updated = await UpdatedAsync(
            admin,
            question.Id,
            FillIn(topic.Id, "<p>Fundada em [[2]].</p>", [Blank(2, "1960", "21 de abril de 1960")]) with { ChangeReason = "Só a data." });

        updated.Status.Should().Be(QuestionStatus.Published);
        BlanksOf(updated.Blanks).Should().ContainSingle().Which.Answers.Should().Equal("1960", "21 de abril de 1960");
    }

    // AC7, BR7: what the type does not have is refused, never ignored; nothing is stored.
    [Fact]
    public async Task Create_WithOptionsAKeyOrMatchItems_IsRefused_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var statement = $"<p>{marker} [[1]]</p>";
        var request = FillIn(topic.Id, statement, [Blank(1, "x")]);

        var options = await CreateAsync(admin, request with { Options = [new SaveQuestionOptionRequest("<p>A</p>")] });
        var key = await CreateAsync(admin, request with { TrueFalseKey = true });
        var items = await CreateAsync(admin, request with { MatchItems = [new SaveMatchItemRequest("<p>Um</p>")] });

        (await RefusalAsync(options)).Should().Be(QuestionBankErrorCodes.QuestionOptionsNotAllowed);
        (await RefusalAsync(key)).Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        (await RefusalAsync(items)).Should().Be(QuestionBankErrorCodes.QuestionMatchItemsNotAllowed);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.TrueFalse)]
    [InlineData(QuestionType.MultipleAnswer)]
    [InlineData(QuestionType.Matching)]
    public async Task Create_AnotherTypeWithBlanks_IsRefusedWithBlanksNotAllowed(QuestionType type)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(
            admin, new SaveQuestionRequest(topic.Id, "pt-BR", "<p>Enunciado [[1]]</p>", Type: type, Blanks: [Blank(1, "x")]));

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionBlanksNotAllowed);
    }

    // AC8, BR2: the type never changes; naming its own type is accepted.
    [Fact]
    public async Task Update_NamingAnotherType_Is409TypeChanged_AndNamingItsOwnIsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, FillIn(topic.Id, blanks: CapitalBlanks()));

        var other = await UpdateAsync(admin, question.Id, Complete(topic.Id, "<p>Mudou</p>") with { Type = QuestionType.SingleChoice });
        var own = await UpdatedAsync(admin, question.Id, FillIn(topic.Id, blanks: [Blank(1, "Brasília"), Blank(2, "1960", "MCMLX")]));

        (await RefusalAsync(other, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        own.Type.Should().Be(QuestionType.FillInTheBlanks);
        BlanksOf(own.Blanks).Last().Answers.Should().Equal("1960", "MCMLX");
    }

    // AC10, BR10: a new accepted answer needs a reason and records a key change with the blanks.
    [Fact]
    public async Task Update_Published_AddingAnAcceptedAnswer_NeedsAReason_AndThenRecordsAKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedFillInAsync(admin, topic.Id);
        var widened = FillIn(topic.Id, blanks: [Blank(1, "Brasília", "Brasilia"), Blank(2, "1960")]);

        var refused = await UpdateAsync(admin, question.Id, widened);

        (await RefusalAsync(refused)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        await UpdatedAsync(admin, question.Id, widened with { ChangeReason = "Aceita a grafia sem acento." });

        var version = await VersionAsync(admin, question.Id, 2);
        version.AnswerKeyChanged.Should().BeTrue();
        version.Reason.Should().Be("Aceita a grafia sem acento.");
        BlanksOf(version.Blanks).First().Answers.Should().Equal("Brasília", "Brasilia");
        BlanksOf((await VersionAsync(admin, question.Id, 1)).Blanks).First().Answers.Should().Equal("Brasília");
    }

    [Fact]
    public async Task Update_Published_AddingOrRemovingABlank_NeedsAReason()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedFillInAsync(admin, topic.Id);

        var added = await UpdateAsync(
            admin, question.Id, FillIn(topic.Id, "<p>[[1]], [[2]] e [[3]].</p>", [.. CapitalBlanks(), Blank(3, "JK")]));
        var removed = await UpdateAsync(admin, question.Id, FillIn(topic.Id, "<p>Capital: [[1]].</p>", [Blank(1, "Brasília")]));

        (await RefusalAsync(added)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await RefusalAsync(removed)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
    }

    // AC11, BR10: text around the markers, their order, and the order or case of the answers are not a key change.
    [Fact]
    public async Task Update_Published_SwappingMarkersAndReorderingAnswers_RecordsAVersionWithNoKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, FillIn(topic.Id, blanks: [Blank(1, "Brasília", "Brasilia"), Blank(2, "1960")]));
        (await PublishAsync(admin, question.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        await UpdatedAsync(
            admin,
            question.Id,
            FillIn(topic.Id, "<p>Fundada em [[2]], a capital federal é [[1]].</p>", [Blank(2, "1960"), Blank(1, "BRASILIA", "brasília")]));

        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(2);
        versions[0].AnswerKeyChanged.Should().BeFalse();
        BlanksOf((await VersionAsync(admin, question.Id, 2)).Blanks).Select(blank => blank.Number).Should().Equal(2, 1);
    }

    // AC12 (F-106 trap T3): editing the linked base text records a version that keeps the blanks.
    [Fact]
    public async Task UpdateBaseText_LinkedToAPublishedFillIn_RecordsAVersionThatKeepsTheBlanks()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var created = await admin.PostAsJsonAsync(
            BaseTexts, new SaveBaseTextRequest($"Texto {Marker()}", "pt-BR", "<p>Brasília nasceu no cerrado.</p>"), AppJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var text = (await created.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
        var question = await PublishedFillInAsync(admin, topic.Id, text.Id);

        var updated = await admin.PutAsJsonAsync(
            $"{BaseTexts}/{text.Id}", new SaveBaseTextRequest(text.Title, "pt-BR", "<p>Brasília nasceu no planalto.</p>"), AppJson.Options);

        updated.StatusCode.Should().Be(HttpStatusCode.OK, await updated.Content.ReadAsStringAsync());
        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(2);
        versions[0].AnswerKeyChanged.Should().BeFalse();
        BlanksOf((await VersionAsync(admin, question.Id, 2)).Blanks).Select(blank => blank.Number).Should().Equal(1, 2);
    }

    // AC12, BR8, BR11: the rules the types share hold for this type.
    [Fact]
    public async Task Create_SanitizesTheStatement_AndKeepsTheMarkers()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        const string hostile = "<p onclick=\"x()\">A capital é [[1]] <sup>2</sup></p><script>alert(1)</script>";

        var question = await CreatedAsync(admin, FillIn(topic.Id, hostile, [Blank(1, "Brasília")]));

        question.StatementHtml.Should().NotContain("script").And.NotContain("onclick").And.Contain("[[1]]").And.Contain("<sup>2</sup>");
        BlanksOf(question.Blanks).Should().ContainSingle();
    }

    [Fact]
    public async Task Create_WithElevenImagesInTheStatement_IsRefusedWithImagesTooMany()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var images = string.Concat(Enumerable.Range(0, 11).Select(_ => $"<img src=\"/question-images/{Guid.CreateVersion7()}\" alt=\"x\">"));

        var response = await CreateAsync(admin, FillIn(topic.Id, $"<p>[[1]] {images}</p>", [Blank(1, "a")]));

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionImagesTooMany);
        BlanksOf((await CreatedAsync(admin, FillIn(topic.Id, "<p>[[1]]</p>", [Blank(1, "a")]))).Blanks)
            .Should().ContainSingle("the same statement without the pictures is stored with its blank");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Create_WithADifficultyOutOfRange_IsRefused(int difficulty)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(admin, FillIn(topic.Id, blanks: CapitalBlanks()) with { Difficulty = difficulty });

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionDifficultyInvalid);
        BlanksOf((await CreatedAsync(admin, FillIn(topic.Id, blanks: CapitalBlanks()) with { Difficulty = 3 })).Blanks)
            .Should().HaveCount(2, "a difficulty in range is stored with the blanks");
    }

    [Fact]
    public async Task Create_WithAPaperNumberAlreadyTaken_IsRefusedWithPaperNumberTaken()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var first = await CreatedAsync(admin, FillIn(topic.Id, blanks: CapitalBlanks()) with { ExamEditionId = edition.Id, PaperNumber = 9 });

        var response = await CreateAsync(admin, FillIn(topic.Id, blanks: CapitalBlanks()) with { ExamEditionId = edition.Id, PaperNumber = 9 });

        (await RefusalAsync(response, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberTaken);
        BlanksOf(first.Blanks).Should().HaveCount(2);
    }

    [Fact]
    public async Task Delete_Published_IsRefused_AndADraftIsDeleted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await PublishedFillInAsync(admin, topic.Id);
        var draft = await CreatedAsync(admin, FillIn(topic.Id, blanks: [Blank(1, "Brasília")]));

        var refused = await admin.DeleteAsync($"{Questions}/{published.Id}");
        var deleted = await admin.DeleteAsync($"{Questions}/{draft.Id}");

        (await RefusalAsync(refused, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTopic_WithOnlyAFillInQuestion_IsRefusedWithTopicInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, FillIn(topic.Id, blanks: CapitalBlanks()));

        var response = await admin.DeleteAsync($"/api/v1/catalog/topics/{topic.Id}");

        (await RefusalAsync(response, HttpStatusCode.Conflict)).Should().Be(CatalogErrorCodes.TopicInUse);
        BlanksOf(question.Blanks).Should().HaveCount(2);
    }

    [Fact]
    public async Task AnnulAndReinstate_KeepTheBlanksInTheirVersions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedFillInAsync(admin, topic.Id);

        var annul = await admin.PostAsJsonAsync($"{Questions}/{question.Id}/annul", new AnnulQuestionRequest("Anulada."), AppJson.Options);
        var reinstate = await admin.PostAsJsonAsync(
            $"{Questions}/{question.Id}/reinstate", new ReinstateQuestionRequest("Recurso indeferido."), AppJson.Options);

        annul.StatusCode.Should().Be(HttpStatusCode.OK, await annul.Content.ReadAsStringAsync());
        reinstate.StatusCode.Should().Be(HttpStatusCode.OK, await reinstate.Content.ReadAsStringAsync());
        BlanksOf((await VersionAsync(admin, question.Id, 2)).Blanks).Select(blank => blank.Number).Should().Equal(1, 2);
        BlanksOf((await VersionAsync(admin, question.Id, 3)).Blanks).Select(blank => blank.Number).Should().Equal(1, 2);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Published);
    }

    [Fact]
    public async Task Submit_CompleteDraft_GoesToReview_AndAnIncompleteOneIsRefused()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var complete = await CreatedAsync(admin, FillIn(topic.Id, blanks: CapitalBlanks()));
        var incomplete = await CreatedAsync(admin, FillIn(topic.Id, blanks: [Blank(1, "Brasília")]));

        var sent = await admin.PostAsync($"{Questions}/{complete.Id}/submit", null);
        var refused = await admin.PostAsync($"{Questions}/{incomplete.Id}/submit", null);

        sent.StatusCode.Should().Be(HttpStatusCode.OK, await sent.Content.ReadAsStringAsync());
        (await GetAsync(admin, complete.Id)).Status.Should().Be(QuestionStatus.InReview);
        (await RefusalAsync(refused)).Should().Be(QuestionBankErrorCodes.QuestionBlankAnswersRequired);
    }

    // AC13, BR9, BR13: every type is named in the list; the excerpt shows four underscores per marker; the other
    // types answer an empty list of blanks.
    [Fact]
    public async Task List_QuestionsOfTheFiveTypes_NameTheTypeOfEachRow_AndTheExcerptHidesTheMarkers()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var single = await CreatedAsync(admin, Complete(topic.Id, $"<p>{marker} escolha</p>"));
        var trueFalse = await CreatedAsync(
            admin, new(topic.Id, "pt-BR", $"<p>{marker} certo</p>", Type: QuestionType.TrueFalse, TrueFalseKey: true));
        var multiple = await CreatedAsync(admin, new(topic.Id, "pt-BR", $"<p>{marker} várias</p>", Type: QuestionType.MultipleAnswer));
        var matching = await CreatedAsync(admin, new(topic.Id, "pt-BR", $"<p>{marker} associação</p>", Type: QuestionType.Matching));
        var fillIn = await CreatedAsync(admin, FillIn(topic.Id, $"<p>{marker} capital: [[1]], ano: [[2]]</p>", CapitalBlanks()));

        var items = (await ListAsync(admin, $"?search={marker}")).Items;

        items.Should().HaveCount(5);
        items.Single(item => item.Id == single.Id).Type.Should().Be(QuestionType.SingleChoice);
        items.Single(item => item.Id == trueFalse.Id).Type.Should().Be(QuestionType.TrueFalse);
        items.Single(item => item.Id == multiple.Id).Type.Should().Be(QuestionType.MultipleAnswer);
        items.Single(item => item.Id == matching.Id).Type.Should().Be(QuestionType.Matching);
        var row = items.Single(item => item.Id == fillIn.Id);
        row.Type.Should().Be(QuestionType.FillInTheBlanks);
        row.StatementExcerpt.Should().Be($"{marker} capital: ____, ano: ____");
        foreach (var other in new[] { single, trueFalse, multiple, matching })
        {
            (await GetAsync(admin, other.Id)).Blanks.Should().NotBeNull().And.BeEmpty();
        }
    }

    [Fact]
    public async Task List_SearchingForAMarker_FindsNothing()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var fillIn = await CreatedAsync(admin, FillIn(topic.Id, $"<p>{marker} [[7]]</p>", [Blank(7, "sete")]));

        (await ListAsync(admin, $"?search={Uri.EscapeDataString($"{marker} [[7]]")}")).Items.Should().BeEmpty();
        (await ListAsync(admin, $"?search={marker}")).Items.Should().ContainSingle().Which.StatementExcerpt.Should().Be($"{marker} ____");
        BlanksOf(fillIn.Blanks).Should().ContainSingle();
    }

    [Fact]
    public async Task FindVersion_OfAnotherType_HasAnEmptyBlanksList()
    {
        var admin = await AdminAsync();
        var question = await PublishedAsync(admin, (await TopicAsync(admin)).Id);

        (await VersionAsync(admin, question.Id, 1)).Blanks.Should().NotBeNull().And.BeEmpty();
    }

    // AC17, BR11: the same permission as every question endpoint.
    [Fact]
    public async Task CreateAndUpdate_WithoutThePermission_AnswerForbidden()
    {
        var student = await StudentAsync();
        var request = FillIn(Guid.CreateVersion7(), blanks: CapitalBlanks());

        var create = await student.PostAsJsonAsync(Questions, request, AppJson.Options);
        var update = await student.PutAsJsonAsync($"{Questions}/{Guid.CreateVersion7()}", request, AppJson.Options);

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The same request by a curator, who holds the permission, is stored with its blanks.
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        BlanksOf((await CreatedAsync(curator, FillIn(topic.Id, blanks: CapitalBlanks()))).Blanks).Should().HaveCount(2);
    }
}
