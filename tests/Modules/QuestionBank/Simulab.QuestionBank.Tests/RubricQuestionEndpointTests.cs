using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-109 through HTTP, on a real PostgreSQL: authoring an open-answer and an essay question with a rubric, the draft
/// and publish rules, the refusals of BR5 to BR8 with the position of the criterion, the key change that needs a
/// reason (BR10), the rules the types share, the base-text fan-out, the list, the responses and the permission.
/// </summary>
public sealed class RubricQuestionEndpointTests : OriginApiTests
{
    private const string BaseTexts = "/api/v1/question-bank/base-texts";
    private const string ThemeStatement = "<p>Discuta o tema proposto.</p>";

    private static SaveCriterionRequest Criterion(string? name, int? points = 10, string? description = null) => new(name, description, points);

    private static SaveQuestionRequest Rubric(
        Guid topicId,
        QuestionType type = QuestionType.Essay,
        string statement = ThemeStatement,
        IReadOnlyList<SaveCriterionRequest>? rubric = null) =>
        new(topicId, "pt-BR", statement, Type: type, Rubric: rubric);

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/publish", null);

    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<IReadOnlyList<QuestionVersionListItemResponse>>(
            $"{Questions}/{id}/versions", AppJson.Options))!;

    private static async Task<QuestionVersionResponse> VersionAsync(HttpClient client, Guid id, int number) =>
        (await client.GetFromJsonAsync<QuestionVersionResponse>($"{Questions}/{id}/versions/{number}", AppJson.Options))!;

    private static async Task<QuestionResponse> PublishedAsync(
        HttpClient admin, Guid topicId, QuestionType type = QuestionType.Essay, Guid? baseTextId = null)
    {
        var question = await CreatedAsync(
            admin, Rubric(topicId, type, rubric: [Criterion("Theme", 30), Criterion("Argument", 40)]) with { BaseTextId = baseTextId });
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

    // The code and the position of a row refusal: for the criterion codes, the position is the criterion's, from 1.
    private static async Task<(string Code, int? Position)> RowRefusalAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        var problem = JsonDocument.Parse(body).RootElement;

        return (
            problem.GetProperty("code").GetString()!,
            problem.TryGetProperty("position", out var position) ? position.GetInt32() : null);
    }

    private static IEnumerable<(int Position, string Name, string? Description, int MaxPoints)> CriteriaOf(IReadOnlyList<CriterionResponse>? rubric) =>
        (rubric ?? []).Select(criterion => (criterion.Position, criterion.Name, criterion.Description, criterion.MaxPoints));

    // AC1, BR1, BR5: a draft of the type, listed with it, with an empty rubric and no size limit.
    [Theory]
    [InlineData(QuestionType.OpenAnswer)]
    [InlineData(QuestionType.Essay)]
    public async Task Create_WithNoRubric_IsStoredAsADraftOfThatType_WithAnEmptyRubric_AndListedWithIt(QuestionType type)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, Rubric(topic.Id, type));

        question.Type.Should().Be(type);
        question.Status.Should().Be(QuestionStatus.Draft);
        question.Options.Should().BeEmpty();
        question.Rubric.Should().NotBeNull().And.BeEmpty();
        question.AnswerSizeMin.Should().BeNull();
        question.AnswerSizeMax.Should().BeNull();
        (await ListAsync(admin, "?pageSize=100")).Items.Single(item => item.Id == question.Id).Type.Should().Be(type);
    }

    // AC2: stored trimmed, in the order written, positions from 1.
    [Fact]
    public async Task Create_WithThreeCriteria_StoresThemTrimmedInOrder()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(
            admin,
            Rubric(topic.Id, QuestionType.OpenAnswer, rubric: [Criterion(" Theme ", 30, " Meets the prompt "), Criterion("Argument", 40), Criterion("Language", 30)]));

        var expected = new[] { (1, "Theme", (string?)"Meets the prompt", 30), (2, "Argument", null, 40), (3, "Language", null, 30) };
        CriteriaOf(question.Rubric).Should().Equal(expected);
        CriteriaOf((await GetAsync(admin, question.Id)).Rubric).Should().Equal(expected);
    }

    // AC3, BR3, BR5: every limit with the position of its criterion; nothing is stored.
    [Fact]
    public async Task Create_BreakingTheLimitsOfTheRubric_IsRefusedWithThePositionOfTheCriterion_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        Task<HttpResponseMessage> CreateWithAsync(params SaveCriterionRequest[] rubric) =>
            CreateAsync(admin, Rubric(topic.Id, QuestionType.Essay, $"<p>{marker}</p>", rubric));

        var eleven = await RowRefusalAsync(await CreateWithAsync([.. Enumerable.Range(1, 11).Select(number => Criterion($"Criterion {number}"))]));
        var noName = await RowRefusalAsync(await CreateWithAsync(Criterion("Theme"), Criterion("  ")));
        var longName = await RowRefusalAsync(await CreateWithAsync(Criterion(new string('a', QuestionLimits.CriterionNameMaxLength + 1))));
        var longDescription = await RowRefusalAsync(
            await CreateWithAsync(Criterion("Theme"), Criterion("Argument", 10, new string('a', QuestionLimits.CriterionDescriptionMaxLength + 1))));
        var noPoints = await RowRefusalAsync(await CreateWithAsync(Criterion("Theme", null)));
        var zeroPoints = await RowRefusalAsync(await CreateWithAsync(Criterion("Theme"), Criterion("Argument", 0)));
        var tooMany = await RowRefusalAsync(await CreateWithAsync(Criterion("Theme", 101)));
        var repeated = await RowRefusalAsync(await CreateWithAsync(Criterion("Theme"), Criterion("Argument"), Criterion(" theme  ")));

        eleven.Code.Should().Be(QuestionBankErrorCodes.QuestionRubricCriteriaCount);
        noName.Should().Be((QuestionBankErrorCodes.QuestionCriterionNameRequired, 2));
        longName.Should().Be((QuestionBankErrorCodes.QuestionCriterionNameTooLong, 1));
        longDescription.Should().Be((QuestionBankErrorCodes.QuestionCriterionDescriptionTooLong, 2));
        noPoints.Should().Be((QuestionBankErrorCodes.QuestionCriterionPointsOutOfRange, 1));
        zeroPoints.Should().Be((QuestionBankErrorCodes.QuestionCriterionPointsOutOfRange, 2));
        tooMany.Should().Be((QuestionBankErrorCodes.QuestionCriterionPointsOutOfRange, 1));
        repeated.Should().Be((QuestionBankErrorCodes.QuestionCriterionNameRepeated, 3));
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithTheLimitsThemselves_IsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var rubric = Enumerable.Range(1, 10)
            .Select(number => Criterion(number == 1 ? new string('a', QuestionLimits.CriterionNameMaxLength) : $"Criterion {number}", number == 1 ? 100 : 1,
                number == 1 ? new string('d', QuestionLimits.CriterionDescriptionMaxLength) : null))
            .ToList();

        var question = await CreatedAsync(admin, Rubric(topic.Id, rubric: rubric));

        question.Rubric.Should().HaveCount(10);
    }

    // AC4, BR6
    [Theory]
    [InlineData(QuestionType.OpenAnswer)]
    [InlineData(QuestionType.Essay)]
    public async Task Publish_WithNoCriterion_IsRefused_AndWithOne_IsPublished(QuestionType type)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var empty = await CreatedAsync(admin, Rubric(topic.Id, type));
        var complete = await CreatedAsync(admin, Rubric(topic.Id, type, rubric: [Criterion("Theme")]));

        (await RefusalAsync(await PublishAsync(admin, empty.Id))).Should().Be(QuestionBankErrorCodes.QuestionRubricCriteriaRequired);
        var response = await PublishAsync(admin, complete.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!.Status.Should().Be(QuestionStatus.Published);
        (await GetAsync(admin, empty.Id)).Status.Should().Be(QuestionStatus.Draft);
    }

    // AC5, BR6: a published question stays complete; a request that omits the rubric counts as an empty one.
    [Fact]
    public async Task Update_Published_RemovingEveryCriterion_IsRefused_AndTheStoredQuestionIsUnchanged_AndAnotherValidRubricStaysPublished()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedAsync(admin, topic.Id);

        var none = await UpdateAsync(admin, question.Id, Rubric(topic.Id, statement: "<p>Outro tema</p>", rubric: []));
        var omitted = await UpdateAsync(admin, question.Id, Rubric(topic.Id, statement: "<p>Outro tema</p>"));

        (await RefusalAsync(none)).Should().Be(QuestionBankErrorCodes.QuestionRubricCriteriaRequired);
        (await RefusalAsync(omitted)).Should().Be(QuestionBankErrorCodes.QuestionRubricCriteriaRequired);
        var stored = await GetAsync(admin, question.Id);
        stored.StatementHtml.Should().Be(ThemeStatement);
        stored.Rubric.Should().HaveCount(2);
        var updated = await UpdatedAsync(
            admin, question.Id, Rubric(topic.Id, rubric: [Criterion("Clarity", 50)]) with { ChangeReason = "Novo critério." });
        updated.Status.Should().Be(QuestionStatus.Published);
        CriteriaOf(updated.Rubric).Should().Equal([(1, "Clarity", null, 50)]);
    }

    // AC6, BR7: an essay in lines, an open answer in characters.
    [Fact]
    public async Task Create_AnEssayWithALineLimit_StoresIt_AndAnInvalidOneIsRefused()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();

        var essay = await CreatedAsync(admin, Rubric(topic.Id) with { AnswerSizeMin = 7, AnswerSizeMax = 30 });
        var onlyMax = await CreatedAsync(admin, Rubric(topic.Id) with { AnswerSizeMax = 30 });
        var inverted = await CreateAsync(admin, Rubric(topic.Id, statement: $"<p>{marker}</p>") with { AnswerSizeMin = 31, AnswerSizeMax = 30 });
        var zero = await CreateAsync(admin, Rubric(topic.Id, statement: $"<p>{marker}</p>") with { AnswerSizeMin = 0 });
        var tooMany = await CreateAsync(admin, Rubric(topic.Id, statement: $"<p>{marker}</p>") with { AnswerSizeMax = 101 });

        (essay.AnswerSizeMin, essay.AnswerSizeMax).Should().Be((7, 30));
        (onlyMax.AnswerSizeMin, onlyMax.AnswerSizeMax).Should().Be((null, 30));
        (await RefusalAsync(inverted)).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeInvalid);
        (await RefusalAsync(zero)).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeInvalid);
        (await RefusalAsync(tooMany)).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeInvalid);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
        var stored = await GetAsync(admin, essay.Id);
        (stored.AnswerSizeMin, stored.AnswerSizeMax).Should().Be((7, 30));
    }

    [Fact]
    public async Task Create_AnOpenAnswerWithACharacterLimit_StoresIt_AndAMinimumOrAnInvalidOneIsRefused()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        SaveQuestionRequest Open(int? min, int? max) =>
            Rubric(topic.Id, QuestionType.OpenAnswer, $"<p>{marker}</p>") with { AnswerSizeMin = min, AnswerSizeMax = max };

        var open = await CreatedAsync(admin, Open(null, 500));
        var longest = await CreatedAsync(admin, Open(null, QuestionLimits.OpenAnswerCharactersMax));
        var withMinimum = await CreateAsync(admin, Open(10, 500));
        var zero = await CreateAsync(admin, Open(null, 0));
        var tooMany = await CreateAsync(admin, Open(null, QuestionLimits.OpenAnswerCharactersMax + 1));

        (open.AnswerSizeMin, open.AnswerSizeMax).Should().Be((null, 500));
        longest.AnswerSizeMax.Should().Be(QuestionLimits.OpenAnswerCharactersMax);
        (await RefusalAsync(withMinimum)).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeInvalid);
        (await RefusalAsync(zero)).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeInvalid);
        (await RefusalAsync(tooMany)).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeInvalid);
    }

    // AC7, BR8: nothing is ignored; nothing is stored.
    [Fact]
    public async Task Create_OfAnotherType_WithARubricOrASize_IsRefused_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();

        var single = await CreateAsync(admin, Complete(topic.Id, $"<p>{marker}</p>") with { Rubric = [Criterion("Theme")] });
        var shortAnswer = await CreateAsync(
            admin, new(topic.Id, "pt-BR", $"<p>{marker}</p>", Type: QuestionType.ShortAnswer, Blanks: [new(1, ["x"])], AnswerSizeMax: 30));

        (await RefusalAsync(single)).Should().Be(QuestionBankErrorCodes.QuestionRubricNotAllowed);
        (await RefusalAsync(shortAnswer)).Should().Be(QuestionBankErrorCodes.QuestionAnswerSizeNotAllowed);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(QuestionType.OpenAnswer)]
    [InlineData(QuestionType.Essay)]
    public async Task Create_WithOptionsAKeyMatchItemsOrBlanks_IsRefused_AndNothingIsStored(QuestionType type)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var request = Rubric(topic.Id, type, $"<p>{marker}</p>", [Criterion("Theme")]);

        var options = await CreateAsync(admin, request with { Options = [new SaveQuestionOptionRequest("<p>A</p>")] });
        var key = await CreateAsync(admin, request with { TrueFalseKey = true });
        var items = await CreateAsync(admin, request with { MatchItems = [new SaveMatchItemRequest("<p>Um</p>")] });
        var blanks = await CreateAsync(admin, request with { Blanks = [new SaveBlankRequest(1, ["x"])] });

        (await RefusalAsync(options)).Should().Be(QuestionBankErrorCodes.QuestionOptionsNotAllowed);
        (await RefusalAsync(key)).Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        (await RefusalAsync(items)).Should().Be(QuestionBankErrorCodes.QuestionMatchItemsNotAllowed);
        (await RefusalAsync(blanks)).Should().Be(QuestionBankErrorCodes.QuestionBlanksNotAllowed);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    // AC8, BR2: the type never changes; naming its own type is accepted.
    [Fact]
    public async Task Update_NamingAnotherType_Is409TypeChanged_AndNamingItsOwnIsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Rubric(topic.Id, QuestionType.OpenAnswer, rubric: [Criterion("Theme")]));

        var other = await UpdateAsync(admin, question.Id, Rubric(topic.Id, QuestionType.Essay, rubric: [Criterion("Theme")]));
        var own = await UpdatedAsync(admin, question.Id, Rubric(topic.Id, QuestionType.OpenAnswer, rubric: [Criterion("Theme"), Criterion("Argument")]));
        var omitted = await UpdatedAsync(admin, question.Id, Rubric(topic.Id, QuestionType.OpenAnswer, rubric: [Criterion("Theme")]) with { Type = null });

        (await RefusalAsync(other, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        own.Type.Should().Be(QuestionType.OpenAnswer);
        own.Rubric.Should().HaveCount(2);
        omitted.Type.Should().Be(QuestionType.OpenAnswer);
    }

    // AC9, BR8: [[1]] is plain text of the statement; the excerpt shows it as typed.
    [Fact]
    public async Task Create_WithStatementTextThatLooksLikeAMarker_KeepsItAsTyped_InTheStatementAndTheExcerpt()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();

        var question = await CreatedAsync(admin, Rubric(topic.Id, statement: $"<p>{marker} escreva [[1]] assim</p>", rubric: [Criterion("Theme")]));

        question.StatementHtml.Should().Contain("[[1]]");
        question.Rubric.Should().ContainSingle();
        (await ListAsync(admin, $"?search={marker}")).Items.Should().ContainSingle()
            .Which.StatementExcerpt.Should().Be($"{marker} escreva [[1]] assim");
    }

    // AC10, BR10: a change of the key needs a reason and records a key change with the rubric.
    [Theory]
    [InlineData(QuestionType.Essay)]
    [InlineData(QuestionType.OpenAnswer)]
    public async Task Update_Published_ChangingThePointsOfACriterion_NeedsAReason_AndThenRecordsAKeyChange(QuestionType type)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedAsync(admin, topic.Id, type);
        var changed = Rubric(topic.Id, type, rubric: [Criterion("Theme", 30), Criterion("Argument", 45)]);

        var refused = await UpdateAsync(admin, question.Id, changed);

        (await RefusalAsync(refused)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        await UpdatedAsync(admin, question.Id, changed with { ChangeReason = "A banca mudou o peso." });

        var version = await VersionAsync(admin, question.Id, 2);
        version.AnswerKeyChanged.Should().BeTrue();
        version.Reason.Should().Be("A banca mudou o peso.");
        CriteriaOf(version.Rubric).Should().Equal([(1, "Theme", null, 30), (2, "Argument", null, 45)]);
        CriteriaOf((await VersionAsync(admin, question.Id, 1)).Rubric).Should().Equal([(1, "Theme", null, 30), (2, "Argument", null, 40)]);
    }

    [Fact]
    public async Task Update_Published_AddingRemovingOrRenamingACriterion_NeedsAReason()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedAsync(admin, topic.Id);

        var added = await UpdateAsync(admin, question.Id, Rubric(topic.Id, rubric: [Criterion("Theme", 30), Criterion("Argument", 40), Criterion("Language", 10)]));
        var removed = await UpdateAsync(admin, question.Id, Rubric(topic.Id, rubric: [Criterion("Theme", 30)]));
        var renamed = await UpdateAsync(admin, question.Id, Rubric(topic.Id, rubric: [Criterion("Theme", 30), Criterion("Reasoning", 40)]));

        (await RefusalAsync(added)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await RefusalAsync(removed)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await RefusalAsync(renamed)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();
    }

    // AC11, BR10: a description, the order, the statement, the explanation or the size are not a key change.
    [Fact]
    public async Task Update_Published_ChangingOnlyDescriptionOrderStatementExplanationOrSize_RecordsVersionsWithNoKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedAsync(admin, topic.Id);

        await UpdatedAsync(admin, question.Id, Rubric(topic.Id, rubric: [Criterion("Theme", 30, "Now described"), Criterion("Argument", 40)]));
        await UpdatedAsync(admin, question.Id, Rubric(topic.Id, rubric: [Criterion("Argument", 40), Criterion("Theme", 30, "Now described")]));
        await UpdatedAsync(admin, question.Id, Rubric(topic.Id, statement: "<p>Outro tema.</p>", rubric: [Criterion("Argument", 40), Criterion("Theme", 30, "Now described")]));
        await UpdatedAsync(
            admin, question.Id,
            Rubric(topic.Id, statement: "<p>Outro tema.</p>", rubric: [Criterion("Argument", 40), Criterion("Theme", 30, "Now described")])
                with { ExplanationHtml = "<p>Modelo de resposta.</p>" });
        await UpdatedAsync(
            admin, question.Id,
            Rubric(topic.Id, statement: "<p>Outro tema.</p>", rubric: [Criterion("Argument", 40), Criterion("Theme", 30, "Now described")])
                with { ExplanationHtml = "<p>Modelo de resposta.</p>", AnswerSizeMin = 7, AnswerSizeMax = 30 });

        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(6);
        for (var number = 2; number <= 6; number++)
        {
            (await VersionAsync(admin, question.Id, number)).AnswerKeyChanged.Should().BeFalse($"version {number} changed no key");
        }

        var last = await VersionAsync(admin, question.Id, 6);
        (last.AnswerSizeMin, last.AnswerSizeMax).Should().Be((7, 30));
        last.Rubric!.Select(criterion => criterion.Name).Should().Equal("Argument", "Theme");
    }

    // AC12 (F-106 trap T3): editing the linked base text records a version that keeps the rubric.
    [Fact]
    public async Task UpdateBaseText_LinkedToAPublishedEssay_RecordsAVersionThatKeepsTheRubric()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var created = await admin.PostAsJsonAsync(
            BaseTexts, new SaveBaseTextRequest($"Texto {Marker()}", "pt-BR", "<p>A cidade cresce.</p>"), AppJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var text = (await created.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
        var question = await PublishedAsync(admin, topic.Id, QuestionType.Essay, text.Id);

        var updated = await admin.PutAsJsonAsync(
            $"{BaseTexts}/{text.Id}", new SaveBaseTextRequest(text.Title, "pt-BR", "<p>A cidade cresce rápido.</p>"), AppJson.Options);

        updated.StatusCode.Should().Be(HttpStatusCode.OK, await updated.Content.ReadAsStringAsync());
        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(2);
        versions[0].AnswerKeyChanged.Should().BeFalse();
        CriteriaOf((await VersionAsync(admin, question.Id, 2)).Rubric).Should().Equal([(1, "Theme", null, 30), (2, "Argument", null, 40)]);
    }

    // AC12, BR11: the rules the types share hold for these types.
    [Fact]
    public async Task Create_SanitizesTheStatement()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        const string hostile = "<p onclick=\"x()\">Tema <sup>2</sup></p><script>alert(1)</script>";

        var question = await CreatedAsync(admin, Rubric(topic.Id, statement: hostile, rubric: [Criterion("Theme")]));

        question.StatementHtml.Should().NotContain("script").And.NotContain("onclick").And.Contain("<sup>2</sup>");
        question.Rubric.Should().ContainSingle();
    }

    [Fact]
    public async Task Create_WithElevenImagesInTheStatement_IsRefusedWithImagesTooMany()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var images = string.Concat(Enumerable.Range(0, 11).Select(_ => $"<img src=\"/question-images/{Guid.CreateVersion7()}\" alt=\"x\">"));

        var response = await CreateAsync(admin, Rubric(topic.Id, statement: $"<p>x {images}</p>", rubric: [Criterion("Theme")]));

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionImagesTooMany);
        (await CreatedAsync(admin, Rubric(topic.Id, statement: "<p>x</p>", rubric: [Criterion("Theme")]))).Rubric
            .Should().ContainSingle("the same statement without the pictures is stored with its rubric");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Create_WithADifficultyOutOfRange_IsRefused(int difficulty)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(admin, Rubric(topic.Id, rubric: [Criterion("Theme")]) with { Difficulty = difficulty });

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionDifficultyInvalid);
        var valid = await CreatedAsync(admin, Rubric(topic.Id, rubric: [Criterion("Theme")]) with { Difficulty = 3 });
        valid.Difficulty.Should().Be(3);
        valid.Rubric.Should().ContainSingle();
    }

    [Fact]
    public async Task Create_WithAPaperNumberAlreadyTaken_IsRefusedWithPaperNumberTaken()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var first = await CreatedAsync(admin, Rubric(topic.Id, rubric: [Criterion("Theme")]) with { ExamEditionId = edition.Id, PaperNumber = 9 });
        first.Rubric.Should().ContainSingle();

        var response = await CreateAsync(admin, Rubric(topic.Id, QuestionType.OpenAnswer, rubric: [Criterion("Theme")]) with { ExamEditionId = edition.Id, PaperNumber = 9 });

        (await RefusalAsync(response, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberTaken);
    }

    [Fact]
    public async Task Delete_Published_IsRefused_AndADraftIsDeleted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await PublishedAsync(admin, topic.Id);
        var draft = await CreatedAsync(admin, Rubric(topic.Id, rubric: [Criterion("Theme")]));

        var refused = await admin.DeleteAsync($"{Questions}/{published.Id}");
        var deleted = await admin.DeleteAsync($"{Questions}/{draft.Id}");

        (await RefusalAsync(refused, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTopic_WithOnlyAnEssayQuestion_IsRefusedWithTopicInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        (await CreatedAsync(admin, Rubric(topic.Id, rubric: [Criterion("Theme")]))).Rubric.Should().ContainSingle();

        var response = await admin.DeleteAsync($"/api/v1/catalog/topics/{topic.Id}");

        (await RefusalAsync(response, HttpStatusCode.Conflict)).Should().Be(CatalogErrorCodes.TopicInUse);
    }

    [Fact]
    public async Task AnnulAndReinstate_KeepTheRubricAndTheSizeInTheirVersions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(
            admin, Rubric(topic.Id, rubric: [Criterion("Theme", 30), Criterion("Argument", 40)]) with { AnswerSizeMin = 7, AnswerSizeMax = 30 });
        (await PublishAsync(admin, question.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var annul = await admin.PostAsJsonAsync($"{Questions}/{question.Id}/annul", new AnnulQuestionRequest("Anulada."), AppJson.Options);
        var reinstate = await admin.PostAsJsonAsync(
            $"{Questions}/{question.Id}/reinstate", new ReinstateQuestionRequest("Recurso indeferido."), AppJson.Options);

        annul.StatusCode.Should().Be(HttpStatusCode.OK, await annul.Content.ReadAsStringAsync());
        reinstate.StatusCode.Should().Be(HttpStatusCode.OK, await reinstate.Content.ReadAsStringAsync());
        foreach (var number in new[] { 2, 3 })
        {
            var version = await VersionAsync(admin, question.Id, number);
            CriteriaOf(version.Rubric).Should().Equal([(1, "Theme", null, 30), (2, "Argument", null, 40)]);
            (version.AnswerSizeMin, version.AnswerSizeMax).Should().Be((7, 30));
        }

        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Published);
    }

    [Fact]
    public async Task Submit_CompleteDraft_GoesToReview_AndAnIncompleteOneIsRefused()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var complete = await CreatedAsync(admin, Rubric(topic.Id, rubric: [Criterion("Theme")]));
        var incomplete = await CreatedAsync(admin, Rubric(topic.Id, QuestionType.OpenAnswer));

        var sent = await admin.PostAsync($"{Questions}/{complete.Id}/submit", null);
        var refused = await admin.PostAsync($"{Questions}/{incomplete.Id}/submit", null);

        sent.StatusCode.Should().Be(HttpStatusCode.OK, await sent.Content.ReadAsStringAsync());
        (await GetAsync(admin, complete.Id)).Status.Should().Be(QuestionStatus.InReview);
        (await RefusalAsync(refused)).Should().Be(QuestionBankErrorCodes.QuestionRubricCriteriaRequired);
    }

    // AC13, BR13: all eight types are named in the list; the other types carry an empty rubric and no size.
    [Fact]
    public async Task List_QuestionsOfTheEightTypes_NameTheTypeOfEachRow_AndOnlyTheTwoCarryARubric()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var single = await CreatedAsync(admin, Complete(topic.Id, $"<p>{marker} escolha</p>"));
        var shortAnswer = await CreatedAsync(
            admin, new(topic.Id, "pt-BR", $"<p>{marker} capital?</p>", Type: QuestionType.ShortAnswer, Blanks: [new(1, ["Brasília"])]));
        var open = await CreatedAsync(admin, Rubric(topic.Id, QuestionType.OpenAnswer, $"<p>{marker} aberta</p>", [Criterion("Theme")]));
        var essay = await CreatedAsync(admin, Rubric(topic.Id, QuestionType.Essay, $"<p>{marker} redação</p>", [Criterion("Theme")]));

        var items = (await ListAsync(admin, $"?search={marker}")).Items;

        items.Should().HaveCount(4);
        items.Single(item => item.Id == single.Id).Type.Should().Be(QuestionType.SingleChoice);
        items.Single(item => item.Id == shortAnswer.Id).Type.Should().Be(QuestionType.ShortAnswer);
        items.Single(item => item.Id == open.Id).Type.Should().Be(QuestionType.OpenAnswer);
        items.Single(item => item.Id == essay.Id).Type.Should().Be(QuestionType.Essay);
        foreach (var other in new[] { single, shortAnswer })
        {
            var stored = await GetAsync(admin, other.Id);
            stored.Rubric.Should().NotBeNull().And.BeEmpty();
            (stored.AnswerSizeMin, stored.AnswerSizeMax).Should().Be((null, null));
        }

        CriteriaOf((await GetAsync(admin, essay.Id)).Rubric).Should().ContainSingle();
    }

    [Fact]
    public async Task VersionsRecordedBeforeTheRubric_ReadAsAnEmptyRubricAndNoSize()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Complete(topic.Id, $"<p>{Marker()}</p>"));
        (await PublishAsync(admin, question.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var version = await VersionAsync(admin, question.Id, 1);

        version.Rubric.Should().NotBeNull().And.BeEmpty();
        (version.AnswerSizeMin, version.AnswerSizeMax).Should().Be((null, null));
    }

    // AC18, BR11: the same permission as every question endpoint.
    [Fact]
    public async Task CreateAndUpdate_WithoutThePermission_AnswerForbidden()
    {
        var student = await StudentAsync();
        var request = Rubric(Guid.CreateVersion7(), rubric: [Criterion("Theme")]);

        var create = await student.PostAsJsonAsync(Questions, request, AppJson.Options);
        var update = await student.PutAsJsonAsync($"{Questions}/{Guid.CreateVersion7()}", request, AppJson.Options);

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The same request by a curator, who holds the permission, is stored with its rubric.
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        CriteriaOf((await CreatedAsync(curator, Rubric(topic.Id, rubric: [Criterion("Theme")]))).Rubric).Should().ContainSingle();
    }
}
